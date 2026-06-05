using CafeOrders.Application.Abstractions;
using CafeOrders.Application.Contracts.Orders;
using CafeOrders.Application.Contracts.Settings;
using CafeOrders.Domain.Entities;
using CafeOrders.Domain.Enums;
using CafeOrders.Infrastructure.Persistence;
using CafeOrders.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CafeOrders.Tests;

public sealed class OrderServiceSoundPlaybackTests
{
    [Fact]
    public async Task MarkSoundPlayedAsync_SetsPersistentPlaybackFields()
    {
        await using var dbContext = CreateDbContext();
        var order = await SeedOrderAsync(dbContext);
        var service = new OrderService(dbContext, new FakeRealtimeNotifier(), new FakeSettingsService());

        var result = await service.MarkSoundPlayedAsync(order.Id);

        Assert.NotNull(result);
        Assert.True(result.IsSoundPlayed);
        Assert.NotNull(result.SoundPlayedAt);

        var persisted = await dbContext.Orders.AsNoTracking().SingleAsync(x => x.Id == order.Id);
        Assert.True(persisted.IsSoundPlayed);
        Assert.NotNull(persisted.SoundPlayedAt);
    }

    [Fact]
    public async Task GetActiveOrdersAsync_WithSoundPendingOnly_ReturnsOnlyUnplayedPendingOrders()
    {
        await using var dbContext = CreateDbContext();
        var pendingUnplayed = await SeedOrderAsync(dbContext, status: OrderStatus.Pending, isSoundPlayed: false);
        await SeedOrderAsync(dbContext, status: OrderStatus.Pending, isSoundPlayed: true);
        await SeedOrderAsync(dbContext, status: OrderStatus.Rejected, isSoundPlayed: false);
        await SeedOrderAsync(dbContext, status: OrderStatus.Completed, isSoundPlayed: false);
        var service = new OrderService(dbContext, new FakeRealtimeNotifier(), new FakeSettingsService());

        var result = await service.GetActiveOrdersAsync(soundPendingOnly: true);

        var order = Assert.Single(result);
        Assert.Equal(pendingUnplayed.Id, order.Id);
        Assert.False(order.IsSoundPlayed);
        Assert.Equal(OrderStatus.Pending.ToString(), order.Status);
    }

    private static async Task<Order> SeedOrderAsync(
        CafeOrdersDbContext dbContext,
        OrderStatus status = OrderStatus.Pending,
        bool isSoundPlayed = false)
    {
        var table = new CafeTable { Name = $"Masa {Guid.NewGuid():N}", IsActive = true };
        var device = new Device
        {
            Id = Guid.NewGuid(),
            HostName = $"PC-{Guid.NewGuid():N}",
            MacAddress = Guid.NewGuid().ToString("N"),
            IpAddress = "192.168.2.30",
            IsApproved = true,
            Table = table
        };
        var product = new Product
        {
            CategoryId = 1,
            Name = $"Urun {Guid.NewGuid():N}",
            Price = 25m,
            IsActive = true
        };
        var order = new Order
        {
            Table = table,
            Device = device,
            Status = status,
            IsSoundPlayed = isSoundPlayed,
            SoundPlayedAt = isSoundPlayed ? DateTime.UtcNow : null,
            OrderLines =
            [
                new OrderLine
                {
                    Product = product,
                    Quantity = 1,
                    UnitPrice = product.Price,
                    LineTotal = product.Price
                }
            ]
        };
        order.RecalculateTotal();

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        return order;
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
        public Task NotifyDeviceApprovedAsync(Device device, string token, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyDeviceRejectedAsync(Device device, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyDeviceMappedAsync(Device device, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task NotifyDevicesUpdatedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

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
