using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CafeManagement.Infrastructure.Persistence;

public sealed class CafeManagementDbContextFactory : IDesignTimeDbContextFactory<CafeManagementDbContext>
{
    public CafeManagementDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CafeManagementDbContext>();
        var connectionString = "Server=.\\SQLEXPRESS;Database=CafeManagement;User Id=CafeManagementAdmin;Password=sa@CafeManagement!;TrustServerCertificate=True;MultipleActiveResultSets=True";

        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
            sqlOptions.MigrationsAssembly(typeof(CafeManagementDbContext).Assembly.FullName));

        return new CafeManagementDbContext(optionsBuilder.Options);
    }
}
