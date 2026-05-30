using CafeManagement.Application.Abstractions;
using CafeManagement.Application.Contracts.Dashboard;
using CafeManagement.Core.Enums;
using CafeManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.Infrastructure.Services;

public sealed class DashboardService(CafeManagementDbContext dbContext, IOrderService orderService, ISettingsService settingsService) : IDashboardService
{
    private static readonly TimeSpan OfflineThreshold = TimeSpan.FromSeconds(35);

    public async Task<DashboardSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var offlineThreshold = now.Subtract(OfflineThreshold);

        await dbContext.Devices
            .Where(x => x.LastSeenAt.HasValue && x.LastSeenAt.Value < offlineThreshold && x.Status != DeviceStatus.Offline)
            .ExecuteUpdateAsync(updates => updates.SetProperty(device => device.Status, DeviceStatus.Offline), cancellationToken);

        var devices = await dbContext.Devices
            .AsNoTracking()
            .OrderBy(x => x.IsApproved)
            .ThenBy(x => x.HostName)
            .ToListAsync(cancellationToken);

        var orders = await orderService.GetActiveOrdersAsync(cancellationToken);
        var revenueToday = await dbContext.Orders
            .Where(x => x.Status == OrderStatus.Completed && x.CompletedAt >= now.Date)
            .SumAsync(x => (decimal?)x.TotalPrice, cancellationToken) ?? 0m;

        return new DashboardSnapshotDto(
            new DashboardStatsDto(
                devices.Count(x => x.Status == DeviceStatus.Online),
                devices.Count(x => !x.IsApproved),
                orders.Count(x => x.Status == OrderStatus.Pending.ToString()),
                revenueToday),
            devices.Select(x => x.ToDto()).ToArray(),
            orders,
            await settingsService.GetActiveInfoMessageAsync(cancellationToken));
    }
}
