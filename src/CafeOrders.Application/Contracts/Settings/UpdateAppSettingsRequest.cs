namespace CafeOrders.Application.Contracts.Settings;

public sealed record UpdateAppSettingsRequest(
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
    string? NewOrderSoundUrl);
