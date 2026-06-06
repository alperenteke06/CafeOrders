using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CafeOrders.Infrastructure.Persistence;

public sealed class CafeOrdersDbContextFactory : IDesignTimeDbContextFactory<CafeOrdersDbContext>
{
    public CafeOrdersDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CafeOrdersDbContext>();
        var configuration = BuildConfiguration();
        var connectionString = configuration.GetConnectionString("CafeOrders")
            ?? "Server=.\\SQLEXPRESS;Database=CafeOrders;User Id=sa;Password=sa@Alperen123!;TrustServerCertificate=True;MultipleActiveResultSets=True";

        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
            sqlOptions.MigrationsAssembly(typeof(CafeOrdersDbContext).Assembly.FullName));

        return new CafeOrdersDbContext(optionsBuilder.Options);
    }

    private static IConfiguration BuildConfiguration()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var basePath = ResolveConfigurationBasePath();

        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();
    }

    private static string ResolveConfigurationBasePath()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var apiPath = Path.Combine(current.FullName, "src", "CafeOrders.API");
            if (File.Exists(Path.Combine(apiPath, "appsettings.json")))
            {
                return apiPath;
            }

            if (File.Exists(Path.Combine(current.FullName, "appsettings.json"))
                && string.Equals(current.Name, "CafeOrders.API", StringComparison.OrdinalIgnoreCase))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
