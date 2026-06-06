using CafeOrders.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeOrders.Infrastructure.Persistence;

public sealed class CafeOrdersDbContext(DbContextOptions<CafeOrdersDbContext> options) : DbContext(options)
{
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<CafeTable> Tables => Set<CafeTable>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<InfoMessage> InfoMessages => Set<InfoMessage>();
    public DbSet<ApplicationLogEntry> ApplicationLogEntries => Set<ApplicationLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>().HasIndex(x => x.MacAddress).IsUnique();
        modelBuilder.Entity<Order>().Property(x => x.TotalPrice).HasPrecision(18, 2);
        modelBuilder.Entity<OrderLine>().Property(x => x.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<OrderLine>().Property(x => x.LineTotal).HasPrecision(18, 2);
        modelBuilder.Entity<Product>().Property(x => x.Price).HasPrecision(18, 2);
        modelBuilder.Entity<AppSetting>().Property(x => x.MinimumOrderAmount).HasPrecision(18, 2);
        modelBuilder.Entity<ApplicationLogEntry>().Property(x => x.Source).HasMaxLength(64);
        modelBuilder.Entity<ApplicationLogEntry>().Property(x => x.Level).HasMaxLength(24);
        modelBuilder.Entity<ApplicationLogEntry>().Property(x => x.Message).HasMaxLength(2000);
        modelBuilder.Entity<ApplicationLogEntry>().Property(x => x.Exception).HasMaxLength(4000);
        modelBuilder.Entity<ApplicationLogEntry>().Property(x => x.Category).HasMaxLength(256);
        modelBuilder.Entity<ApplicationLogEntry>().Property(x => x.MachineName).HasMaxLength(128);
        modelBuilder.Entity<ApplicationLogEntry>().Property(x => x.DeviceKey).HasMaxLength(128);
        modelBuilder.Entity<ApplicationLogEntry>().HasIndex(x => x.CreatedAtUtc);
        modelBuilder.Entity<ApplicationLogEntry>().HasIndex(x => new { x.Source, x.CreatedAtUtc });
        modelBuilder.Entity<ApplicationLogEntry>().HasIndex(x => new { x.Level, x.CreatedAtUtc });
    }
}
