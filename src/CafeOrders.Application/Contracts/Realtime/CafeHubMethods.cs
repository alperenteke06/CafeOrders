namespace CafeOrders.Application.Contracts.Realtime;

public static class CafeHubMethods
{
    public const string JoinDeviceChannel = nameof(JoinDeviceChannel);
    public const string JoinAdminChannel = nameof(JoinAdminChannel);
    public const string ReportOrderSoundPlaybackStarted = nameof(ReportOrderSoundPlaybackStarted);
    public const string AcknowledgeOrderSound = nameof(AcknowledgeOrderSound);
}
