using CafeOrders.Application.Abstractions;
using CafeOrders.Application.Contracts.Devices;
using CafeOrders.Application.Contracts.Orders;
using CafeOrders.Application.Contracts.Settings;
using CafeOrders.Domain.Entities;
using CafeOrders.Infrastructure.Persistence;
using CafeOrders.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CafeOrders.Tests;

public sealed class DeviceServiceRealtimeTests
{
    [Fact]
    public async Task RegisterAsync_NewDevice_NotifiesAdminDevicesUpdated()
    {
        await using var dbContext = CreateDbContext();
        var notifier = new FakeRealtimeNotifier();
        var service = new DeviceService(dbContext, new FakeJwtTokenService(), notifier);

        var response = await service.RegisterAsync(new DeviceRegistrationRequest("PC-01", "AA:BB:CC:DD:EE:FF", "192.168.2.30"));

        Assert.False(response.IsApproved);
        Assert.Equal(1, notifier.DevicesUpdatedCount);
    }

    [Fact]
    public async Task ApproveAsync_SendsApprovalToDeviceAndDevicesUpdatedToAdmin()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Tables.Add(new CafeTable { Id = 1, Name = "Masa 01", IsActive = true });
        dbContext.Devices.Add(new Device
        {
            Id = Guid.NewGuid(),
            HostName = "PC-01",
            MacAddress = "aabbccddeeff",
            IpAddress = "192.168.2.30"
        });
        await dbContext.SaveChangesAsync();

        var deviceId = await dbContext.Devices.Select(x => x.Id).SingleAsync();
        var notifier = new FakeRealtimeNotifier();
        var service = new DeviceService(dbContext, new FakeJwtTokenService(), notifier);

        var response = await service.ApproveAsync(new ApproveDeviceRequest(deviceId, 1));

        Assert.NotNull(response);
        Assert.True(response.IsApproved);
        Assert.Equal("device-token", response.Token);
        Assert.Equal(1, notifier.DeviceApprovedCount);
        Assert.Equal(1, notifier.DevicesUpdatedCount);
    }

    private static CafeOrdersDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CafeOrdersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new CafeOrdersDbContext(options);
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public string CreateDeviceToken(Device device) => "device-token";
    }

    private sealed class FakeRealtimeNotifier : IRealtimeNotifier
    {
        public int DeviceApprovedCount { get; private set; }
        public int DevicesUpdatedCount { get; private set; }

        public Task NotifyDeviceApprovedAsync(Device device, string token, CancellationToken cancellationToken = default)
        {
            DeviceApprovedCount++;
            return Task.CompletedTask;
        }

        public Task NotifyDeviceRejectedAsync(Device device, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyDeviceMappedAsync(Device device, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyDevicesUpdatedAsync(CancellationToken cancellationToken = default)
        {
            DevicesUpdatedCount++;
            return Task.CompletedTask;
        }

        public Task NotifyOrderCreatedAsync(OrderDto order, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyOrderAcceptedAsync(Device device, OrderDto order, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyOrderRejectedAsync(Device device, OrderDto order, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyOrderCompletedAsync(Device device, OrderDto order, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyCatalogUpdatedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyTablesUpdatedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyAppSettingsUpdatedAsync(AppSettingsDto settings, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyInfoMessageUpdatedAsync(InfoMessageDto infoMessage, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
