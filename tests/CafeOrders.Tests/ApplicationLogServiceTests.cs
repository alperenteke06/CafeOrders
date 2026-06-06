using CafeOrders.Application.Abstractions;
using CafeOrders.Application.Contracts.Logging;
using CafeOrders.Application.Contracts.Orders;
using CafeOrders.Application.Contracts.Settings;
using CafeOrders.Domain.Entities;
using CafeOrders.Infrastructure.Persistence;
using CafeOrders.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace CafeOrders.Tests;

public sealed class ApplicationLogServiceTests
{
    [Fact]
    public void ApplicationLogEntriesMigration_IsRegisteredForCafeOrdersDbContext()
    {
        var options = new DbContextOptionsBuilder<CafeOrdersDbContext>()
            .UseSqlServer("Server=(local);Database=CafeOrders_Test;TrustServerCertificate=True")
            .Options;

        using var dbContext = new CafeOrdersDbContext(options);
        var migrations = dbContext.Database.GetMigrations();

        Assert.Contains("20260606120000_AddApplicationLogEntries", migrations);
    }

    [Fact]
    public async Task CreateAsync_PersistsLogAndNotifiesAdmins()
    {
        await using var dbContext = CreateDbContext();
        var notifier = new FakeRealtimeNotifier();
        var service = new ApplicationLogService(dbContext, notifier);

        var created = await service.CreateAsync(new ApplicationLogCreateRequest(
            "DesktopApp",
            "Warn",
            "Kiosk config parse recovered",
            Category: "CafeOrders.DesktopApp",
            MachineName: "PC-01",
            DeviceKey: "aabbccddeeff",
            TableId: 2,
            OrderId: 42));

        var persisted = await dbContext.ApplicationLogEntries.AsNoTracking().SingleAsync();
        Assert.Equal("DesktopApp", persisted.Source);
        Assert.Equal("Warning", persisted.Level);
        Assert.Equal("Kiosk config parse recovered", persisted.Message);
        Assert.Equal(42, persisted.OrderId);
        Assert.Equal(created.Id, notifier.LastLog?.Id);
        Assert.Equal(1, notifier.ApplicationLogCreatedCount);
    }

    [Fact]
    public async Task GetRecentAsync_FiltersBySourceLevelAndSearch()
    {
        await using var dbContext = CreateDbContext();
        var service = new ApplicationLogService(dbContext, new FakeRealtimeNotifier());

        await service.CreateAsync(new ApplicationLogCreateRequest("API", "Info", "Health check OK", MachineName: "SERVER"));
        await service.CreateAsync(new ApplicationLogCreateRequest("WebUI", "Error", "Upload failed for product image", MachineName: "SERVER"));
        await service.CreateAsync(new ApplicationLogCreateRequest("DesktopApp", "Warning", "Media path fallback used", MachineName: "PC-02"));

        var result = await service.GetRecentAsync(source: "WebUI", level: "Error", search: "Upload", take: 50);

        var log = Assert.Single(result);
        Assert.Equal("WebUI", log.Source);
        Assert.Equal("Error", log.Level);
        Assert.Contains("Upload failed", log.Message);
    }

    private static CafeOrdersDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CafeOrdersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new CafeOrdersDbContext(options);
    }

    private sealed class FakeRealtimeNotifier : IRealtimeNotifier
    {
        public int ApplicationLogCreatedCount { get; private set; }
        public ApplicationLogDto? LastLog { get; private set; }

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

        public Task NotifyApplicationLogCreatedAsync(ApplicationLogDto log, CancellationToken cancellationToken = default)
        {
            ApplicationLogCreatedCount++;
            LastLog = log;
            return Task.CompletedTask;
        }
    }
}
