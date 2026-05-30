using CafeManagement.Application.Abstractions;
using CafeManagement.Application.Contracts.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace CafeManagement.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
public sealed class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("snapshot")]
    public Task<DashboardSnapshotDto> Snapshot(CancellationToken cancellationToken)
        => dashboardService.GetSnapshotAsync(cancellationToken);
}
