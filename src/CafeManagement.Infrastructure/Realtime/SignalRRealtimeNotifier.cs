using CafeManagement.Application.Abstractions;
using CafeManagement.Application.Contracts.Orders;
using CafeManagement.Application.Contracts.Realtime;
using CafeManagement.Application.Contracts.Settings;
using CafeManagement.Core.Entities;
using Microsoft.AspNetCore.SignalR;

namespace CafeManagement.Infrastructure.Realtime;

public sealed class SignalRRealtimeNotifier(IHubContext<CafeHub> hubContext) : IRealtimeNotifier
{
    private static long RealtimeVersion() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public Task NotifyDeviceApprovedAsync(Device device, string token, CancellationToken cancellationToken = default)
    {
        var payload = new { deviceId = device.Id, tableId = device.TableId, token, message = "Masa onaylandi. Ana ekran aciliyor." };
        return Task.WhenAll(
            hubContext.Clients.Group(device.DeviceKey).SendAsync(
            CafeHubEvents.DeviceApproved,
            payload,
            cancellationToken),
            hubContext.Clients.Group("admin").SendAsync(CafeHubEvents.DeviceApproved, payload, cancellationToken));
    }

    public Task NotifyDeviceRejectedAsync(Device device, CancellationToken cancellationToken = default)
    {
        var payload = new { deviceId = device.Id, message = "Cihaz talebi reddedildi." };
        return Task.WhenAll(
            hubContext.Clients.Group(device.DeviceKey).SendAsync(CafeHubEvents.DeviceRejected, payload, cancellationToken),
            hubContext.Clients.Group("admin").SendAsync(CafeHubEvents.DeviceRejected, payload, cancellationToken));
    }

    public Task NotifyDeviceMappedAsync(Device device, CancellationToken cancellationToken = default)
    {
        var payload = new { deviceId = device.Id, tableId = device.TableId, hostName = device.HostName };
        return Task.WhenAll(
            hubContext.Clients.Group("admin").SendAsync(CafeHubEvents.DeviceMapped, payload, cancellationToken),
            hubContext.Clients.Group(device.DeviceKey).SendAsync(CafeHubEvents.DeviceMapped, payload, cancellationToken));
    }

    public Task NotifyDevicesUpdatedAsync(CancellationToken cancellationToken = default)
        => hubContext.Clients.Group("admin").SendAsync(CafeHubEvents.DevicesUpdated, RealtimeVersion(), cancellationToken);

    public Task NotifyOrderCreatedAsync(OrderDto order, CancellationToken cancellationToken = default)
        => hubContext.Clients.Group("admin").SendAsync(CafeHubEvents.OrderCreated, order, cancellationToken);

    public Task NotifyOrderAcceptedAsync(Device device, OrderDto order, string message, CancellationToken cancellationToken = default)
        => Task.WhenAll(
            hubContext.Clients.Group(device.DeviceKey).SendAsync(CafeHubEvents.OrderAccepted, new { order, message }, cancellationToken),
            hubContext.Clients.Group("admin").SendAsync(CafeHubEvents.OrderAccepted, new { order, message }, cancellationToken));

    public Task NotifyOrderRejectedAsync(Device device, OrderDto order, string message, CancellationToken cancellationToken = default)
        => Task.WhenAll(
            hubContext.Clients.Group(device.DeviceKey).SendAsync(CafeHubEvents.OrderRejected, new { order, message }, cancellationToken),
            hubContext.Clients.Group("admin").SendAsync(CafeHubEvents.OrderRejected, new { order, message }, cancellationToken));

    public Task NotifyOrderCompletedAsync(Device device, OrderDto order, CancellationToken cancellationToken = default)
        => Task.WhenAll(
            hubContext.Clients.Group(device.DeviceKey).SendAsync(CafeHubEvents.OrderCompleted, order, cancellationToken),
            hubContext.Clients.Group("admin").SendAsync(CafeHubEvents.OrderCompleted, order, cancellationToken));

    public Task NotifyCatalogUpdatedAsync(CancellationToken cancellationToken = default)
        => hubContext.Clients.All.SendAsync(CafeHubEvents.CatalogUpdated, RealtimeVersion(), cancellationToken);

    public Task NotifyTablesUpdatedAsync(CancellationToken cancellationToken = default)
        => hubContext.Clients.All.SendAsync(CafeHubEvents.TablesUpdated, RealtimeVersion(), cancellationToken);

    public Task NotifyAppSettingsUpdatedAsync(AppSettingsDto settings, CancellationToken cancellationToken = default)
        => hubContext.Clients.All.SendAsync(CafeHubEvents.AppSettingsUpdated, settings, cancellationToken);

    public Task NotifyInfoMessageUpdatedAsync(InfoMessageDto infoMessage, CancellationToken cancellationToken = default)
        => hubContext.Clients.All.SendAsync(CafeHubEvents.InfoMessageUpdated, infoMessage, cancellationToken);
}
