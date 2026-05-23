using CafeOrders.Application.Abstractions;
using CafeOrders.Application.Contracts.Settings;
using CafeOrders.Domain.Entities;
using CafeOrders.Domain.Enums;
using CafeOrders.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace CafeOrders.Infrastructure.Services;

public sealed class SettingsService(
    CafeOrdersDbContext dbContext,
    IRealtimeNotifier realtimeNotifier,
    IConfiguration configuration) : ISettingsService
{
    private readonly string _developerName = configuration["Branding:AppDeveloperName"] ?? "Alperen TEKE";
    private readonly string _developerPhone = configuration["Branding:AppDeveloperPhone"] ?? "0 (541) 688 88 06";

    public async Task<AppSettingsDto> GetAppSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await dbContext.AppSettings
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstAsync(cancellationToken);
        return new AppSettingsDto(
            settings.CafeName,
            _developerName,
            _developerPhone,
            settings.OrderAcceptedMessage,
            settings.OrderRejectedMessage,
            settings.ClientInfoBoxMessage,
            settings.ClientInfoBoxType.ToString(),
            settings.ClientInfoBoxIcon,
            settings.EnableNewOrderSound,
            settings.EnableQuickApproveMode,
            settings.EnableLiveAnnouncements,
            settings.NewOrderSoundUrl);
    }

    public async Task<AppSettingsDto> UpdateAppSettingsAsync(UpdateAppSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var settings = await dbContext.AppSettings
            .OrderBy(x => x.Id)
            .FirstAsync(cancellationToken);
        settings.CafeName = request.CafeName.Trim();
        settings.OrderAcceptedMessage = request.OrderAcceptedMessage.Trim();
        settings.OrderRejectedMessage = request.OrderRejectedMessage.Trim();
        settings.ClientInfoBoxMessage = request.ClientInfoBoxMessage.Trim();
        settings.ClientInfoBoxType = Enum.TryParse<Domain.Enums.InfoMessageType>(request.ClientInfoBoxType, true, out var clientInfoType)
            ? clientInfoType
            : Domain.Enums.InfoMessageType.Info;
        settings.ClientInfoBoxIcon = string.IsNullOrWhiteSpace(request.ClientInfoBoxIcon) ? "campaign" : request.ClientInfoBoxIcon.Trim();
        settings.EnableNewOrderSound = request.EnableNewOrderSound;
        settings.EnableQuickApproveMode = request.EnableQuickApproveMode;
        settings.EnableLiveAnnouncements = request.EnableLiveAnnouncements;
        settings.NewOrderSoundUrl = string.IsNullOrWhiteSpace(request.NewOrderSoundUrl) ? null : request.NewOrderSoundUrl.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);
        var dto = new AppSettingsDto(
            settings.CafeName,
            _developerName,
            _developerPhone,
            settings.OrderAcceptedMessage,
            settings.OrderRejectedMessage,
            settings.ClientInfoBoxMessage,
            settings.ClientInfoBoxType.ToString(),
            settings.ClientInfoBoxIcon,
            settings.EnableNewOrderSound,
            settings.EnableQuickApproveMode,
            settings.EnableLiveAnnouncements,
            settings.NewOrderSoundUrl);
        await realtimeNotifier.NotifyAppSettingsUpdatedAsync(dto, cancellationToken);
        return dto;
    }

    public async Task<InfoMessageDto?> GetActiveInfoMessageAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var infoMessage = await dbContext.InfoMessages
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.IsActive, cancellationToken);
        return infoMessage is not null && infoMessage.IsCurrentlyActive(now) ? infoMessage.ToDto() : null;
    }

    public async Task<InfoMessageDto> UpsertInfoMessageAsync(UpdateInfoMessageRequest request, CancellationToken cancellationToken = default)
    {
        var infoMessage = await dbContext.InfoMessages.OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken) ?? new InfoMessage();
        if (infoMessage.Id == 0)
        {
            await dbContext.InfoMessages.AddAsync(infoMessage, cancellationToken);
        }

        infoMessage.Message = request.Message;
        infoMessage.IsActive = request.IsActive;
        infoMessage.StartDate = request.StartDate;
        infoMessage.EndDate = request.EndDate;
        infoMessage.Type = Enum.TryParse<InfoMessageType>(request.Type, true, out var parsedType) ? parsedType : InfoMessageType.Info;
        infoMessage.IconKey = string.IsNullOrWhiteSpace(request.IconKey) ? "campaign" : request.IconKey.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);

        var dto = infoMessage.ToDto();
        await realtimeNotifier.NotifyInfoMessageUpdatedAsync(dto, cancellationToken);
        return dto;
    }
}
