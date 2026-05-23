using CafeOrders.Application.Abstractions;
using CafeOrders.Application.Contracts.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrders.API.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
public sealed class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("snapshot")]
    public Task<DashboardSnapshotDto> Snapshot(CancellationToken cancellationToken)
        => dashboardService.GetSnapshotAsync(cancellationToken);
}
