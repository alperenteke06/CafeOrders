using CafeManagement.Application.Contracts.Dashboard;

namespace CafeManagement.Application.Abstractions;

public interface IDashboardService
{
    Task<DashboardSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
