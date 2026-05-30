using System.Text.Json;
using CafeManagement.Application.Contracts.Realtime;
using CafeManagement.Application.Contracts.Settings;
using Microsoft.AspNetCore.SignalR.Client;

namespace CafeManagement.Kiosk.Services;

public sealed class RealtimeClient
{
    private HubConnection? _connection;

    public async Task ConnectAsync(
        string hubUrl,
        string deviceKey,
        Func<string, string?, int?, Task> onApproved,
        Action<string> onRejected,
        Action<string, string> onOrderEvent,
        Action<InfoMessageDto> onInfoMessageUpdated,
        Action<AppSettingsDto> onAppSettingsUpdated,
        Func<Task>? onCatalogUpdated = null,
        Func<Task>? onTablesUpdated = null)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<JsonElement>(CafeHubEvents.DeviceApproved, payload =>
        {
            var token = payload.TryGetProperty("token", out var tokenElement) ? tokenElement.GetString() : string.Empty;
            var message = payload.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : null;
            int? tableId = payload.TryGetProperty("tableId", out var tableElement) && tableElement.ValueKind == JsonValueKind.Number
                ? tableElement.GetInt32()
                : null;
            return onApproved(token ?? string.Empty, message, tableId);
        });

        _connection.On<JsonElement>(CafeHubEvents.DeviceRejected, payload =>
        {
            var message = payload.TryGetProperty("message", out var messageElement)
                ? messageElement.GetString()
                : "Cihaz talebi reddedildi.";
            onRejected(message ?? "Cihaz talebi reddedildi.");
        });

        _connection.On<JsonElement>(CafeHubEvents.OrderAccepted, payload =>
        {
            var message = payload.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : "Siparis onaylandi.";
            onOrderEvent(CafeHubEvents.OrderAccepted, message ?? "Siparis onaylandi.");
        });

        _connection.On<JsonElement>(CafeHubEvents.OrderRejected, payload =>
        {
            var message = payload.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : "Siparis reddedildi.";
            onOrderEvent(CafeHubEvents.OrderRejected, message ?? "Siparis reddedildi.");
        });

        _connection.On<InfoMessageDto>(CafeHubEvents.InfoMessageUpdated, payload => onInfoMessageUpdated(payload));

        _connection.On<AppSettingsDto>(CafeHubEvents.AppSettingsUpdated, payload => onAppSettingsUpdated(payload));

        if (onCatalogUpdated is not null)
        {
            _connection.On<long>(CafeHubEvents.CatalogUpdated, _ => onCatalogUpdated());
        }

        if (onTablesUpdated is not null)
        {
            _connection.On<long>(CafeHubEvents.TablesUpdated, _ => onTablesUpdated());
            _connection.On<JsonElement>(CafeHubEvents.DeviceMapped, _ => onTablesUpdated());
        }

        _connection.On<JsonElement>(CafeHubEvents.OrderCompleted, payload =>
        {
            var message = payload.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : "Siparisiniz hazir.";
            onOrderEvent(CafeHubEvents.OrderCompleted, message ?? "Siparisiniz hazir.");
        });

        await _connection.StartAsync();
        await _connection.InvokeAsync(CafeHubMethods.JoinDeviceChannel, deviceKey);
        _connection.Reconnected += async _ =>
        {
            await _connection.InvokeAsync(CafeHubMethods.JoinDeviceChannel, deviceKey);
        };
    }

    public async Task DisconnectAsync()
    {
        if (_connection is null)
        {
            return;
        }

        try
        {
            if (_connection.State is HubConnectionState.Connected or HubConnectionState.Connecting or HubConnectionState.Reconnecting)
            {
                await _connection.StopAsync();
            }
        }
        finally
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
