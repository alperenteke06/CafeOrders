using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CafeOrders.Infrastructure.Persistence;

public sealed class CafeOrdersDbContextFactory : IDesignTimeDbContextFactory<CafeOrdersDbContext>
{
    public CafeOrdersDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CafeOrdersDbContext>();
        var connectionString = "Server=BIL10ALPEREN-NB\\SQLEXPRESS;Database=CafeOrders;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
            sqlOptions.MigrationsAssembly(typeof(CafeOrdersDbContext).Assembly.FullName));

        return new CafeOrdersDbContext(optionsBuilder.Options);
    }
}
