using CafeOrders.Application.Abstractions;
using CafeOrders.Application.Contracts.Orders;
using CafeOrders.Application.Contracts.Settings;
using CafeOrders.Domain.Entities;
using CafeOrders.Infrastructure.Persistence;
using CafeOrders.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CafeOrders.Tests;

public sealed class OrderServiceMinimumAmountTests
{
    [Fact]
    public async Task CreateAsync_RejectsCartBelowMinimumOrderAmount()
    {
        await using var dbContext = CreateDbContext();
        var deviceId = Guid.NewGuid();
        dbContext.AppSettings.Add(new AppSetting { MinimumOrderAmount = 100m });
        dbContext.Tables.Add(new CafeTable { Id = 1, Name = "Masa 01", IsActive = true });
        dbContext.Devices.Add(new Device { Id = deviceId, HostName = "PC-01", MacAddress = "aabbccddeeff", IpAddress = "192.168.2.30", IsApproved = true, TableId = 1 });
        dbContext.Products.Add(new Product { Id = 1, CategoryId = 1, Name = "Damla Su", Price = 40m, IsActive = true });
        await dbContext.SaveChangesAsync();

        var service = new OrderService(dbContext, new FakeRealtimeNotifier(), new FakeSettingsService());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateOrderRequest(deviceId, 1, [new CreateOrderLineRequest(1, 2)])));

        Assert.Contains("Minimum siparis tutari", exception.Message);
        Assert.Empty(dbContext.Orders);
    }

    [Fact]
    public async Task CreateAsync_AllowsCartWhenMinimumOrderAmountIsEmpty()
    {
        await using var dbContext = CreateDbContext();
        var deviceId = Guid.NewGuid();
        dbContext.AppSettings.Add(new AppSetting());
        dbContext.Tables.Add(new CafeTable { Id = 1, Name = "Masa 01", IsActive = true });
        dbContext.Devices.Add(new Device { Id = deviceId, HostName = "PC-01", MacAddress = "aabbccddeeff", IpAddress = "192.168.2.30", IsApproved = true, TableId = 1 });
        dbContext.Products.Add(new Product { Id = 1, CategoryId = 1, Name = "Damla Su", Price = 40m, IsActive = true });
        await dbContext.SaveChangesAsync();

        var notifier = new FakeRealtimeNotifier();
        var service = new OrderService(dbContext, notifier, new FakeSettingsService());

        var order = await service.CreateAsync(new CreateOrderRequest(deviceId, 1, [new CreateOrderLineRequest(1, 2)]));

        Assert.Equal(80m, order.TotalPrice);
        Assert.Equal(1, notifier.OrderCreatedCount);
    }

    private static CafeOrdersDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CafeOrdersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new CafeOrdersDbContext(options);
    }

    private sealed class FakeSettingsService : ISettingsService
    {
        public Task<AppSettingsDto> GetAppSettingsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppSettingsDto(
                "Cafe",
                "Developer",
                "Phone",
                "Accepted",
                "Rejected",
                "Info",
                "Info",
                "campaign",
                true,
                true,
                true,
                null,
                null));

        public Task<AppSettingsDto> UpdateAppSettingsAsync(UpdateAppSettingsRequest request, CancellationToken cancellationToken = default)
            => GetAppSettingsAsync(cancellationToken);

        public Task<InfoMessageDto?> GetActiveInfoMessageAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<InfoMessageDto?>(null);

        public Task<InfoMessageDto> UpsertInfoMessageAsync(UpdateInfoMessageRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new InfoMessageDto(1, request.Message, request.Type, request.IconKey, request.IsActive, request.StartDate, request.EndDate));
    }

    private sealed class FakeRealtimeNotifier : IRealtimeNotifier
    {
        public int OrderCreatedCount { get; private set; }

        public Task NotifyDeviceApprovedAsync(Device device, string token, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyDeviceRejectedAsync(Device device, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyDeviceMappedAsync(Device device, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyDevicesUpdatedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyOrderCreatedAsync(OrderDto order, CancellationToken cancellationToken = default)
        {
            OrderCreatedCount++;
            return Task.CompletedTask;
        }

        public Task NotifyOrderAcceptedAsync(Device device, OrderDto order, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyOrderRejectedAsync(Device device, OrderDto order, string message, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyOrderCompletedAsync(Device device, OrderDto order, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyCatalogUpdatedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyTablesUpdatedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyAppSettingsUpdatedAsync(AppSettingsDto settings, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyInfoMessageUpdatedAsync(InfoMessageDto infoMessage, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
