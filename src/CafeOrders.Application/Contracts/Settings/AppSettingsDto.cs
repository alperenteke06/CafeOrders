namespace CafeOrders.Application.Contracts.Settings;

public sealed record AppSettingsDto(
    string CafeName,
    string AppDeveloperName,
    string AppDeveloperPhone,
    string OrderAcceptedMessage,
    string OrderRejectedMessage,
    string ClientInfoBoxMessage,
    string ClientInfoBoxType,
    string ClientInfoBoxIcon,
    bool EnableNewOrderSound,
    bool EnableQuickApproveMode,
    bool EnableLiveAnnouncements,
    decimal? MinimumOrderAmount,
    string? NewOrderSoundUrl);
