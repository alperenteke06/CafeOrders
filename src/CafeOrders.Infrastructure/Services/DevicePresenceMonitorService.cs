using CafeOrders.Application.Abstractions;
using CafeOrders.Domain.Enums;
using CafeOrders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CafeOrders.Infrastructure.Services;

public sealed class DevicePresenceMonitorService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan OfflineThreshold = TimeSpan.FromSeconds(35);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(ScanInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MarkOfflineDevicesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
            {
                break;
            }
        }
    }

    private async Task MarkOfflineDevicesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeOrdersDbContext>();
        var realtimeNotifier = scope.ServiceProvider.GetRequiredService<IRealtimeNotifier>();

        var offlineThreshold = DateTime.UtcNow.Subtract(OfflineThreshold);
        var changedRows = await dbContext.Devices
            .Where(x => x.IsApproved && x.LastSeenAt.HasValue && x.LastSeenAt.Value < offlineThreshold && x.Status != DeviceStatus.Offline)
            .ExecuteUpdateAsync(updates => updates.SetProperty(device => device.Status, DeviceStatus.Offline), cancellationToken);

        if (changedRows > 0)
        {
            await realtimeNotifier.NotifyTablesUpdatedAsync(cancellationToken);
        }
    }
}
