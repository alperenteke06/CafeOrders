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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>().HasIndex(x => x.MacAddress).IsUnique();
        modelBuilder.Entity<Order>().Property(x => x.TotalPrice).HasPrecision(18, 2);
        modelBuilder.Entity<OrderLine>().Property(x => x.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<OrderLine>().Property(x => x.LineTotal).HasPrecision(18, 2);
        modelBuilder.Entity<Product>().Property(x => x.Price).HasPrecision(18, 2);
    }
}
