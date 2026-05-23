using CafeOrders.Application.Abstractions;
using CafeOrders.Application.Contracts.Settings;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrders.API.Controllers;

[ApiController]
[Route("api/v1/settings")]
public sealed class SettingsController(ISettingsService settingsService) : ControllerBase
{
    [HttpGet("app")]
    public Task<AppSettingsDto> GetAppSettings(CancellationToken cancellationToken)
        => settingsService.GetAppSettingsAsync(cancellationToken);

    [HttpPut("app")]
    public Task<AppSettingsDto> UpdateAppSettings([FromBody] UpdateAppSettingsRequest request, CancellationToken cancellationToken)
        => settingsService.UpdateAppSettingsAsync(request, cancellationToken);

    [HttpGet("info-message")]
    public async Task<IActionResult> GetActiveInfoMessage(CancellationToken cancellationToken)
    {
        var result = await settingsService.GetActiveInfoMessageAsync(cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("info-message")]
    public Task<InfoMessageDto> UpdateInfoMessage([FromBody] UpdateInfoMessageRequest request, CancellationToken cancellationToken)
        => settingsService.UpsertInfoMessageAsync(request, cancellationToken);
}
