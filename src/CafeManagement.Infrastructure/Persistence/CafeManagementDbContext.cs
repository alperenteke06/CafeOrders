using CafeManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.Infrastructure.Persistence;

public sealed class CafeManagementDbContext(DbContextOptions<CafeManagementDbContext> options) : DbContext(options)
{
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<CafeTable> Tables => Set<CafeTable>();
    public DbSet<TableGroup> TableGroups => Set<TableGroup>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<InfoMessage> InfoMessages => Set<InfoMessage>();
    public DbSet<PriceProfile> PriceProfiles => Set<PriceProfile>();
    public DbSet<PriceRule> PriceRules => Set<PriceRule>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<MemberTransaction> MemberTransactions => Set<MemberTransaction>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionEvent> SessionEvents => Set<SessionEvent>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CashRegisterTransaction> CashRegisterTransactions => Set<CashRegisterTransaction>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Branch>().HasIndex(x => x.Code).IsUnique().HasFilter("[Code] IS NOT NULL");
        modelBuilder.Entity<Branch>().Property(x => x.Name).HasMaxLength(160);
        modelBuilder.Entity<Branch>().Property(x => x.Code).HasMaxLength(40);

        modelBuilder.Entity<TableGroup>().Property(x => x.Name).HasMaxLength(120);
        modelBuilder.Entity<TableGroup>()
            .HasOne(x => x.Branch)
            .WithMany(x => x.TableGroups)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CafeTable>()
            .HasOne(x => x.Branch)
            .WithMany(x => x.Tables)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<CafeTable>()
            .HasOne(x => x.TableGroup)
            .WithMany(x => x.Tables)
            .HasForeignKey(x => x.TableGroupId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Device>().HasIndex(x => x.MacAddress).IsUnique();
        modelBuilder.Entity<Order>().Property(x => x.TotalPrice).HasPrecision(18, 2);
        modelBuilder.Entity<Order>()
            .HasOne(x => x.Session)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<OrderLine>().Property(x => x.UnitPrice).HasPrecision(18, 2);
        modelBuilder.Entity<OrderLine>().Property(x => x.LineTotal).HasPrecision(18, 2);
        modelBuilder.Entity<Product>().Property(x => x.Price).HasPrecision(18, 2);

        modelBuilder.Entity<PriceProfile>().Property(x => x.Name).HasMaxLength(120);
        modelBuilder.Entity<PriceRule>().Property(x => x.Name).HasMaxLength(120);
        modelBuilder.Entity<PriceRule>().Property(x => x.PricePerMinute).HasPrecision(18, 4);
        modelBuilder.Entity<PriceRule>()
            .HasOne(x => x.PriceProfile)
            .WithMany(x => x.Rules)
            .HasForeignKey(x => x.PriceProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Member>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Member>().Property(x => x.Code).HasMaxLength(60);
        modelBuilder.Entity<Member>().Property(x => x.FullName).HasMaxLength(160);
        modelBuilder.Entity<Member>().Property(x => x.Balance).HasPrecision(18, 2);
        modelBuilder.Entity<MemberTransaction>().Property(x => x.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<MemberTransaction>().Property(x => x.BalanceAfter).HasPrecision(18, 2);
        modelBuilder.Entity<MemberTransaction>()
            .HasOne(x => x.Member)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Session>().Property(x => x.SessionAmount).HasPrecision(18, 2);
        modelBuilder.Entity<Session>()
            .HasOne(x => x.Branch)
            .WithMany(x => x.Sessions)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Session>()
            .HasOne(x => x.Table)
            .WithMany(x => x.Sessions)
            .HasForeignKey(x => x.TableId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Session>()
            .HasOne(x => x.Device)
            .WithMany()
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Session>()
            .HasOne(x => x.Member)
            .WithMany(x => x.Sessions)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Session>()
            .HasOne(x => x.PriceProfile)
            .WithMany(x => x.Sessions)
            .HasForeignKey(x => x.PriceProfileId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<SessionEvent>()
            .HasOne(x => x.Session)
            .WithMany(x => x.Events)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Payment>().Property(x => x.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<Payment>()
            .HasOne(x => x.Session)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Payment>()
            .HasOne(x => x.Order)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Payment>()
            .HasOne(x => x.Member)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CashRegisterTransaction>().Property(x => x.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<CashRegisterTransaction>()
            .HasOne(x => x.Payment)
            .WithMany(x => x.CashRegisterTransactions)
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AuditLog>().Property(x => x.EntityName).HasMaxLength(160);
        modelBuilder.Entity<AuditLog>().Property(x => x.ActorUserName).HasMaxLength(120);
    }
}
