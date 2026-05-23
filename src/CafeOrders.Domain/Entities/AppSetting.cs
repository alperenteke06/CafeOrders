using CafeOrders.Domain.Enums;

namespace CafeOrders.Domain.Entities;

public sealed class AppSetting
{
    public int Id { get; set; }
    public string CafeName { get; set; } = "NightByte Lounge";
    public string AppDeveloperName { get; set; } = "Alperen TEKE";
    public string AppDeveloperPhone { get; set; } = "0 (541) 688 88 06";
    public string OrderAcceptedMessage { get; set; } = "Siparisiniz mutfaga iletildi.";
    public string OrderRejectedMessage { get; set; } = "Siparisiniz su an isleme alinamadi.";
    public string ClientInfoBoxMessage { get; set; } = "Hos geldiniz. Masaniz hazir oldugunda ekran otomatik acilacak.";
    public InfoMessageType ClientInfoBoxType { get; set; } = InfoMessageType.Info;
    public string ClientInfoBoxIcon { get; set; } = "campaign";
    public bool EnableNewOrderSound { get; set; } = true;
    public bool EnableQuickApproveMode { get; set; } = true;
    public bool EnableLiveAnnouncements { get; set; } = true;
    public string? NewOrderSoundUrl { get; set; }
}
