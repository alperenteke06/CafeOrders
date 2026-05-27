using CafeOrders.Application.Abstractions;
using CafeOrders.Application.Contracts.Devices;
using CafeOrders.Domain.Entities;
using CafeOrders.Domain.Enums;
using CafeOrders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CafeOrders.Infrastructure.Services;

public sealed class DeviceService(
    CafeOrdersDbContext dbContext,
    IJwtTokenService jwtTokenService,
    IRealtimeNotifier realtimeNotifier) : IDeviceService
{
    public async Task<DeviceRegistrationResponse> RegisterAsync(DeviceRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedMac = NormalizeMac(request.MacAddress);
        var device = await dbContext.Devices.FirstOrDefaultAsync(x => x.MacAddress == normalizedMac, cancellationToken);
        var isNewDevice = device is null;
        var wasOnline = device?.Status == DeviceStatus.Online;

        if (device is null)
        {
            device = new Device
            {
                HostName = request.HostName,
                MacAddress = normalizedMac,
                IpAddress = request.IpAddress
            };

            await dbContext.Devices.AddAsync(device, cancellationToken);
        }
        else
        {
            device.HostName = request.HostName;
            device.IpAddress = request.IpAddress;
        }

        if (device.IsApproved)
        {
            device.Status = DeviceStatus.Online;
            device.LastSeenAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        if (isNewDevice || wasOnline != (device.Status == DeviceStatus.Online))
        {
            await realtimeNotifier.NotifyDevicesUpdatedAsync(cancellationToken);
        }

        var token = device.IsApproved ? jwtTokenService.CreateDeviceToken(device) : null;
        return new DeviceRegistrationResponse(device.Id, device.IsApproved, device.DeviceKey, token, device.IsApproved ? "Cihaz dogrulandi." : "Cihaz onay bekliyor.", device.TableId);
    }

    public async Task<DeviceRegistrationResponse?> ApproveAsync(ApproveDeviceRequest request, CancellationToken cancellationToken = default)
    {
        var device = await dbContext.Devices.FirstOrDefaultAsync(x => x.Id == request.DeviceId, cancellationToken);
        if (device is null)
        {
            return null;
        }

        if (request.TableId.HasValue)
        {
            var tableExists = await dbContext.Tables.AnyAsync(x => x.Id == request.TableId.Value && x.IsActive, cancellationToken);
            if (!tableExists)
            {
                throw new InvalidOperationException("Atanmak istenen masa bulunamadi ya da aktif degil.");
            }
        }

        device.IsApproved = true;
        device.Status = DeviceStatus.Online;
        device.LastSeenAt = DateTime.UtcNow;
        device.TableId = request.TableId ?? await GetNextAvailableTableIdAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var token = jwtTokenService.CreateDeviceToken(device);
        await realtimeNotifier.NotifyDeviceApprovedAsync(device, token, cancellationToken);
        await realtimeNotifier.NotifyDevicesUpdatedAsync(cancellationToken);
        return new DeviceRegistrationResponse(device.Id, true, device.DeviceKey, token, "Cihaz onaylandi.", device.TableId);
    }

    public async Task<DeviceRegistrationResponse?> AssignTableAsync(AssignDeviceTableRequest request, CancellationToken cancellationToken = default)
    {
        var device = await dbContext.Devices.FirstOrDefaultAsync(x => x.Id == request.DeviceId, cancellationToken);
        if (device is null)
        {
            return null;
        }

        var wasApproved = device.IsApproved;

        if (request.TableId.HasValue)
        {
            var tableExists = await dbContext.Tables.AnyAsync(x => x.Id == request.TableId.Value && x.IsActive, cancellationToken);
            if (!tableExists)
            {
                throw new InvalidOperationException("Secilen masa bulunamadi ya da aktif degil.");
            }
        }

        device.TableId = request.TableId;
        if (request.TableId.HasValue && !device.IsApproved)
        {
            device.IsApproved = true;
            device.Status = DeviceStatus.Online;
            device.LastSeenAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.NotifyDeviceMappedAsync(device, cancellationToken);
        await realtimeNotifier.NotifyDevicesUpdatedAsync(cancellationToken);

        if (!wasApproved && device.IsApproved)
        {
            var approvalToken = jwtTokenService.CreateDeviceToken(device);
            await realtimeNotifier.NotifyDeviceApprovedAsync(device, approvalToken, cancellationToken);
            return new DeviceRegistrationResponse(device.Id, true, device.DeviceKey, approvalToken, "Cihaz eslestirmesi guncellendi.", device.TableId);
        }

        var token = device.IsApproved ? jwtTokenService.CreateDeviceToken(device) : null;
        return new DeviceRegistrationResponse(device.Id, device.IsApproved, device.DeviceKey, token, "Cihaz eslestirmesi guncellendi.", device.TableId);
    }

    public async Task<bool> RejectAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        var device = await dbContext.Devices.FirstOrDefaultAsync(x => x.Id == deviceId, cancellationToken);
        if (device is null)
        {
            return false;
        }

        dbContext.Devices.Remove(device);
        await dbContext.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.NotifyDeviceRejectedAsync(device, cancellationToken);
        await realtimeNotifier.NotifyDevicesUpdatedAsync(cancellationToken);
        return true;
    }

    public async Task<bool> HeartbeatAsync(HeartbeatRequest request, CancellationToken cancellationToken = default)
    {
        var device = await dbContext.Devices.FirstOrDefaultAsync(x => x.Id == request.DeviceId, cancellationToken);
        if (device is null)
        {
            return false;
        }

        var statusChanged = device.Status != DeviceStatus.Online;
        device.LastSeenAt = DateTime.UtcNow;
        device.Status = DeviceStatus.Online;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (statusChanged)
        {
            await realtimeNotifier.NotifyTablesUpdatedAsync(cancellationToken);
            await realtimeNotifier.NotifyDevicesUpdatedAsync(cancellationToken);
        }

        return true;
    }

    private async Task<int?> GetNextAvailableTableIdAsync(CancellationToken cancellationToken)
    {
        var occupiedIds = await dbContext.Devices.Where(x => x.IsApproved && x.TableId.HasValue).Select(x => x.TableId!.Value).ToListAsync(cancellationToken);
        return await dbContext.Tables.Where(x => x.IsActive && !occupiedIds.Contains(x.Id)).OrderBy(x => x.Id).Select(x => (int?)x.Id).FirstOrDefaultAsync(cancellationToken);
    }

    private static string NormalizeMac(string macAddress) => macAddress.Replace(":", string.Empty).Replace("-", string.Empty).Trim().ToLowerInvariant();
}
