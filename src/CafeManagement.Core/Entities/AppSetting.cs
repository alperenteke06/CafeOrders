using CafeManagement.Core.Enums;

namespace CafeManagement.Core.Entities;

public sealed class AppSetting
{
    public int Id { get; set; }
    public string CafeName { get; set; } = "JetNet E-Spor Arena";
    public string AppDeveloperName { get; set; } = "Alperen TEKE";
    public string AppDeveloperPhone { get; set; } = "0 (541) 688 88 06";
    public string OrderAcceptedMessage { get; set; } = "Siparişiniz başarıyla alınmıştır.";
    public string OrderRejectedMessage { get; set; } = "Siparisiniz su an isleme alinamadi.";
    public string ClientInfoBoxMessage { get; set; } = "İşletme kuralları gereği dışarıdan yiyecek ve içecek getirilmesi yasaktır.";
    public InfoMessageType ClientInfoBoxType { get; set; } = InfoMessageType.Warning;
    public string ClientInfoBoxIcon { get; set; } = "warning";
    public bool EnableNewOrderSound { get; set; } = true;
    public bool EnableQuickApproveMode { get; set; } = true;
    public bool EnableLiveAnnouncements { get; set; } = true;
    public string? NewOrderSoundUrl { get; set; }
}
