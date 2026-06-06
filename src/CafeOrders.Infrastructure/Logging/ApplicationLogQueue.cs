using System.Threading.Channels;
using CafeOrders.Application.Contracts.Logging;
using CafeOrders.Domain.Entities;
using CafeOrders.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CafeOrders.Infrastructure.Logging;

public interface IApplicationLogQueue
{
    bool TryWrite(ApplicationLogCreateRequest request);
    bool TryRead(out ApplicationLogCreateRequest request);
    IAsyncEnumerable<ApplicationLogCreateRequest> ReadAllAsync(CancellationToken cancellationToken);
}

public sealed class ApplicationLogQueue : IApplicationLogQueue
{
    private readonly Channel<ApplicationLogCreateRequest> _channel;

    public ApplicationLogQueue(int capacity = 2000)
    {
        _channel = Channel.CreateBounded<ApplicationLogCreateRequest>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public bool TryWrite(ApplicationLogCreateRequest request)
        => _channel.Writer.TryWrite(request);

    public bool TryRead(out ApplicationLogCreateRequest request)
        => _channel.Reader.TryRead(out request!);

    public IAsyncEnumerable<ApplicationLogCreateRequest> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}

public sealed class ApplicationLogWriterService(
    IApplicationLogQueue queue,
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime lifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var buffer = new List<ApplicationLogCreateRequest>(50);

        await foreach (var item in queue.ReadAllAsync(stoppingToken))
        {
            buffer.Add(item);
            while (buffer.Count < 50 && queue.TryRead(out var next))
            {
                buffer.Add(next);
            }

            await FlushAsync(buffer, stoppingToken);
            buffer.Clear();
        }
    }

    private async Task FlushAsync(IReadOnlyCollection<ApplicationLogCreateRequest> requests, CancellationToken cancellationToken)
    {
        if (requests.Count == 0 || lifetime.ApplicationStopping.IsCancellationRequested)
        {
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Persistence.CafeOrdersDbContext>();
            var realtimeNotifier = scope.ServiceProvider.GetRequiredService<Application.Abstractions.IRealtimeNotifier>();

            var entries = requests.Select(MapRequest).ToArray();
            dbContext.ApplicationLogEntries.AddRange(entries);
            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var entry in entries)
            {
                await realtimeNotifier.NotifyApplicationLogCreatedAsync(ApplicationLogService.Map(entry), cancellationToken);
            }
        }
        catch
        {
            // Centralized logging must never break API/WebUI hosting.
        }
    }

    private static ApplicationLogEntry MapRequest(ApplicationLogCreateRequest request)
        => new()
        {
            CreatedAtUtc = request.CreatedAtUtc?.ToUniversalTime() ?? DateTime.UtcNow,
            Source = NormalizeSource(request.Source),
            Level = NormalizeLevel(request.Level),
            Message = Truncate(request.Message, 2000) ?? string.Empty,
            Exception = Truncate(request.Exception, 4000),
            Category = Truncate(request.Category, 256),
            MachineName = Truncate(request.MachineName, 128),
            DeviceKey = Truncate(request.DeviceKey, 128),
            TableId = request.TableId,
            OrderId = request.OrderId
        };

    private static string NormalizeSource(string? source)
        => string.IsNullOrWhiteSpace(source) ? "Unknown" : Truncate(source, 64) ?? "Unknown";

    private static string NormalizeLevel(string? level)
    {
        var value = string.IsNullOrWhiteSpace(level) ? "Info" : level.Trim();
        return value switch
        {
            "Information" => "Info",
            "Warn" => "Warning",
            _ => Truncate(value, 24) ?? "Info"
        };
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}

public static class ApplicationLogLoggerExtensions
{
    public static ILoggingBuilder AddApplicationLogQueue(
        this ILoggingBuilder logging,
        IConfiguration configuration,
        string source,
        IApplicationLogQueue queue)
    {
        var isEnabled = configuration.GetValue<bool?>("Logging:Centralized:Enabled") ?? true;
        if (isEnabled)
        {
            logging.AddProvider(new ApplicationLogLoggerProvider(source, queue));
        }

        return logging;
    }
}

public sealed class ApplicationLogLoggerProvider(string source, IApplicationLogQueue queue) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
        => new ApplicationLogLogger(source, categoryName, queue);

    public void Dispose()
    {
    }
}

public sealed class ApplicationLogLogger(string source, string categoryName, IApplicationLogQueue queue) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel)
        => ShouldLog(logLevel, categoryName);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        try
        {
            var message = formatter(state, exception);
            if (string.IsNullOrWhiteSpace(message) && exception is null)
            {
                return;
            }

            queue.TryWrite(new ApplicationLogCreateRequest(
                source,
                NormalizeLevel(logLevel),
                message,
                exception?.ToString(),
                categoryName,
                Environment.MachineName,
                CreatedAtUtc: DateTime.UtcNow));
        }
        catch
        {
            // Logging must never interrupt the app.
        }
    }

    private static bool ShouldLog(LogLevel logLevel, string categoryName)
    {
        if (logLevel is LogLevel.None or LogLevel.Trace or LogLevel.Debug)
        {
            return false;
        }

        if (logLevel >= LogLevel.Warning)
        {
            return true;
        }

        return categoryName.StartsWith("CafeOrders.", StringComparison.OrdinalIgnoreCase)
            && !categoryName.StartsWith("CafeOrders.Infrastructure.Logging.", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeLevel(LogLevel logLevel)
        => logLevel switch
        {
            LogLevel.Warning => "Warning",
            LogLevel.Error => "Error",
            LogLevel.Critical => "Critical",
            LogLevel.Information => "Info",
            _ => logLevel.ToString()
        };
}
