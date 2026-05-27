namespace CafeOrders.Application.Contracts.Realtime;

public static class CafeHubEvents
{
    public const string DeviceApproved = nameof(DeviceApproved);
    public const string DeviceRejected = nameof(DeviceRejected);
    public const string DeviceMapped = nameof(DeviceMapped);
    public const string DevicesUpdated = nameof(DevicesUpdated);
    public const string OrderCreated = nameof(OrderCreated);
    public const string OrderAccepted = nameof(OrderAccepted);
    public const string OrderRejected = nameof(OrderRejected);
    public const string OrderCompleted = nameof(OrderCompleted);
    public const string CatalogUpdated = nameof(CatalogUpdated);
    public const string TablesUpdated = nameof(TablesUpdated);
    public const string AppSettingsUpdated = nameof(AppSettingsUpdated);
    public const string InfoMessageUpdated = nameof(InfoMessageUpdated);
}
