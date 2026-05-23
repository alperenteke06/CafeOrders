using CafeOrders.Application.Contracts.Orders;
using CafeOrders.Application.Contracts.Settings;
using CafeOrders.Domain.Entities;

namespace CafeOrders.Application.Abstractions;

public interface IRealtimeNotifier
{
    Task NotifyDeviceApprovedAsync(Device device, string token, CancellationToken cancellationToken = default);
    Task NotifyDeviceRejectedAsync(Guid deviceId, CancellationToken cancellationToken = default);
    Task NotifyDeviceMappedAsync(Device device, CancellationToken cancellationToken = default);
    Task NotifyOrderCreatedAsync(OrderDto order, CancellationToken cancellationToken = default);
    Task NotifyOrderAcceptedAsync(Device device, OrderDto order, string message, CancellationToken cancellationToken = default);
    Task NotifyOrderRejectedAsync(Device device, OrderDto order, string message, CancellationToken cancellationToken = default);
    Task NotifyOrderCompletedAsync(Device device, OrderDto order, CancellationToken cancellationToken = default);
    Task NotifyCatalogUpdatedAsync(CancellationToken cancellationToken = default);
    Task NotifyTablesUpdatedAsync(CancellationToken cancellationToken = default);
    Task NotifyAppSettingsUpdatedAsync(AppSettingsDto settings, CancellationToken cancellationToken = default);
    Task NotifyInfoMessageUpdatedAsync(InfoMessageDto infoMessage, CancellationToken cancellationToken = default);
}
