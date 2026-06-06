using CafeOrders.Application.Abstractions;
using CafeOrders.Application.Contracts.Logging;
using CafeOrders.Domain.Entities;
using CafeOrders.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CafeOrders.Infrastructure.Services;

public sealed class ApplicationLogService(CafeOrdersDbContext dbContext, IRealtimeNotifier realtimeNotifier) : IApplicationLogService
{
    private static readonly HashSet<string> KnownSources = new(StringComparer.OrdinalIgnoreCase)
    {
        "API",
        "WebUI",
        "DesktopApp",
        "AdminAudioAgent"
    };

    private static readonly HashSet<string> KnownLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "Trace",
        "Debug",
        "Info",
        "Information",
        "Warn",
        "Warning",
        "Error",
        "Critical"
    };

    public async Task<ApplicationLogDto> CreateAsync(ApplicationLogCreateRequest request, CancellationToken cancellationToken = default)
    {
        var entry = new ApplicationLogEntry
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

        try
        {
            dbContext.ApplicationLogEntries.Add(entry);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsMissingApplicationLogTable(ex))
        {
            dbContext.ChangeTracker.Clear();
            return Map(entry);
        }

        var dto = Map(entry);
        await realtimeNotifier.NotifyApplicationLogCreatedAsync(dto, cancellationToken);
        return dto;
    }

    public async Task<IReadOnlyCollection<ApplicationLogDto>> GetRecentAsync(
        string? source = null,
        string? level = null,
        string? search = null,
        int take = 200,
        CancellationToken cancellationToken = default)
    {
        var normalizedSource = NormalizeOptionalSource(source);
        var normalizedLevel = NormalizeOptionalLevel(level);
        var normalizedSearch = search?.Trim();
        var safeTake = Math.Clamp(take, 1, 500);

        var query = dbContext.ApplicationLogEntries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedSource))
        {
            query = query.Where(x => x.Source == normalizedSource);
        }

        if (!string.IsNullOrWhiteSpace(normalizedLevel))
        {
            query = normalizedLevel switch
            {
                "Warning" => query.Where(x => x.Level == "Warning" || x.Level == "Warn"),
                "Info" => query.Where(x => x.Level == "Info" || x.Level == "Information"),
                _ => query.Where(x => x.Level == normalizedLevel)
            };
        }

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(x =>
                x.Message.Contains(normalizedSearch) ||
                (x.Exception != null && x.Exception.Contains(normalizedSearch)) ||
                (x.Category != null && x.Category.Contains(normalizedSearch)) ||
                (x.MachineName != null && x.MachineName.Contains(normalizedSearch)) ||
                (x.DeviceKey != null && x.DeviceKey.Contains(normalizedSearch)) ||
                (x.OrderId != null && x.OrderId.Value.ToString().Contains(normalizedSearch)));
        }

        ApplicationLogEntry[] entries;
        try
        {
            entries = await query
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(safeTake)
                .ToArrayAsync(cancellationToken);
        }
        catch (Exception ex) when (IsMissingApplicationLogTable(ex))
        {
            return Array.Empty<ApplicationLogDto>();
        }

        return entries.Select(Map).ToArray();
    }

    public static ApplicationLogDto Map(ApplicationLogEntry entry)
        => new(
            entry.Id,
            entry.CreatedAtUtc,
            entry.Source,
            entry.Level,
            entry.Message,
            entry.Exception,
            entry.Category,
            entry.MachineName,
            entry.DeviceKey,
            entry.TableId,
            entry.OrderId);

    private static string NormalizeSource(string? source)
    {
        var value = string.IsNullOrWhiteSpace(source) ? "Unknown" : source.Trim();
        return KnownSources.TryGetValue(value, out var known) ? known : Truncate(value, 64) ?? "Unknown";
    }

    private static string? NormalizeOptionalSource(string? source)
    {
        if (string.IsNullOrWhiteSpace(source) || string.Equals(source, "all", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return NormalizeSource(source);
    }

    private static string NormalizeLevel(string? level)
    {
        var value = string.IsNullOrWhiteSpace(level) ? "Info" : level.Trim();
        if (!KnownLevels.TryGetValue(value, out var known))
        {
            return "Info";
        }

        return known switch
        {
            "Information" => "Info",
            "Warn" => "Warning",
            _ => known
        };
    }

    private static string? NormalizeOptionalLevel(string? level)
    {
        if (string.IsNullOrWhiteSpace(level) || string.Equals(level, "all", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return NormalizeLevel(level);
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

    private static bool IsMissingApplicationLogTable(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is SqlException sqlException && sqlException.Number == 208)
            {
                return true;
            }

            if (current.Message.Contains("ApplicationLogEntries", StringComparison.OrdinalIgnoreCase)
                && current.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
