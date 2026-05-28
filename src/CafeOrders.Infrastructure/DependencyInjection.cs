using System.Text;
using CafeOrders.Application.Abstractions;
using CafeOrders.Infrastructure.Options;
using CafeOrders.Infrastructure.Persistence;
using CafeOrders.Infrastructure.Realtime;
using CafeOrders.Infrastructure.Security;
using CafeOrders.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CafeOrders.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCafeOrdersInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var connectionString = configuration.GetConnectionString("CafeOrders")
            ?? "Server=.\\SQLEXPRESS;Database=CafeOrders;User Id=CafeOrdersAdmin;Password=sa@CafeOrders!;TrustServerCertificate=True;MultipleActiveResultSets=True";
        services.AddDbContext<CafeOrdersDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
                sqlOptions.MigrationsAssembly(typeof(CafeOrdersDbContext).Assembly.FullName)));

        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ITableService, TableService>();
        services.AddScoped<IAdminAuthService, AdminAuthService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();
        services.AddHostedService<DevicePresenceMonitorService>();
        services.AddScoped<IPasswordHasher<CafeOrders.Domain.Entities.AdminUser>, PasswordHasher<CafeOrders.Domain.Entities.AdminUser>>();
        services.AddSignalR();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = key
                };
            });

        services.AddAuthorization();
        return services;
    }
}
