using CafeOrders.Application.Abstractions;
using CafeOrders.Domain.Enums;
using CafeOrders.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CafeOrders.Infrastructure.Realtime;

public sealed class CafeHub(CafeOrdersDbContext dbContext, IRealtimeNotifier realtimeNotifier) : Hub
{
    public async Task JoinDeviceChannel(string deviceKey)
    {
        var normalizedDeviceKey = deviceKey.Trim().ToLowerInvariant();
        await Groups.AddToGroupAsync(Context.ConnectionId, normalizedDeviceKey);

        var device = await dbContext.Devices.FirstOrDefaultAsync(x => x.MacAddress == normalizedDeviceKey);
        if (device is null)
        {
            return;
        }

        var wasOffline = device.IsApproved && device.Status == DeviceStatus.Offline;
        device.ConnectionId = Context.ConnectionId;
        if (device.IsApproved)
        {
            device.Status = DeviceStatus.Online;
            device.LastSeenAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync();

        if (wasOffline)
        {
            await realtimeNotifier.NotifyTablesUpdatedAsync();
        }
    }

    public Task JoinAdminChannel()
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, "admin");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var device = await dbContext.Devices.FirstOrDefaultAsync(x => x.ConnectionId == Context.ConnectionId);
        if (device is not null)
        {
            device.ConnectionId = null;
            if (device.IsApproved && device.Status != DeviceStatus.Offline)
            {
                device.Status = DeviceStatus.Offline;
                await dbContext.SaveChangesAsync();
                await realtimeNotifier.NotifyTablesUpdatedAsync();
            }
            else
            {
                await dbContext.SaveChangesAsync();
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
