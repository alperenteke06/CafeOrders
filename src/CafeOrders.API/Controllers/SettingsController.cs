using CafeOrders.Application.Abstractions;
using CafeOrders.Application.Contracts.Settings;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrders.API.Controllers;

[ApiController]
[Route("api/v1/settings")]
public sealed class SettingsController(ISettingsService settingsService, ILogger<SettingsController> logger) : ControllerBase
{
    [HttpGet("app")]
    public Task<AppSettingsDto> GetAppSettings(CancellationToken cancellationToken)
        => settingsService.GetAppSettingsAsync(cancellationToken);

    [HttpPut("app")]
    public async Task<AppSettingsDto> UpdateAppSettings([FromBody] UpdateAppSettingsRequest request, CancellationToken cancellationToken)
    {
        var settings = await settingsService.UpdateAppSettingsAsync(request, cancellationToken);
        logger.LogInformation(
            "Application settings updated. CafeName={CafeName}, SoundEnabled={SoundEnabled}, QuickApprove={QuickApprove}, LiveAnnouncements={LiveAnnouncements}, MinimumOrderAmount={MinimumOrderAmount}",
            settings.CafeName,
            settings.EnableNewOrderSound,
            settings.EnableQuickApproveMode,
            settings.EnableLiveAnnouncements,
            settings.MinimumOrderAmount);
        return settings;
    }

    [HttpGet("info-message")]
    public async Task<IActionResult> GetActiveInfoMessage(CancellationToken cancellationToken)
    {
        var result = await settingsService.GetActiveInfoMessageAsync(cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("info-message")]
    public async Task<InfoMessageDto> UpdateInfoMessage([FromBody] UpdateInfoMessageRequest request, CancellationToken cancellationToken)
    {
        var infoMessage = await settingsService.UpsertInfoMessageAsync(request, cancellationToken);
        logger.LogInformation("Info message updated. InfoMessageId={InfoMessageId}, Type={Type}, IconKey={IconKey}, IsActive={IsActive}", infoMessage.Id, infoMessage.Type, infoMessage.IconKey, infoMessage.IsActive);
        return infoMessage;
    }
}
