using CafeOrders.Application.Contracts.Dashboard;

namespace CafeOrders.Application.Abstractions;

public interface IDashboardService
{
    Task<DashboardSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
