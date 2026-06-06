using CafeOrders.Application.Abstractions;
using CafeOrders.Application.Contracts.Logging;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrders.API.Controllers;

[ApiController]
[Route("api/v1/logs")]
public sealed class LogsController(IApplicationLogService applicationLogService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<ApplicationLogDto>> Get(
        [FromQuery] string? source,
        [FromQuery] string? level,
        [FromQuery] string? search,
        [FromQuery] int? take,
        CancellationToken cancellationToken)
        => applicationLogService.GetRecentAsync(source, level, search, take ?? 200, cancellationToken);

    [HttpPost("client")]
    public async Task<IActionResult> CreateClientLog(
        [FromBody] ApplicationLogCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Source) || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Log kaynagi ve mesaji zorunludur." });
        }

        return Ok(await applicationLogService.CreateAsync(request, cancellationToken));
    }
}
