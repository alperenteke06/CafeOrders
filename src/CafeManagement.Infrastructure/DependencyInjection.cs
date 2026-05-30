using System.Text;
using CafeManagement.Application.Abstractions;
using CafeManagement.Infrastructure.Options;
using CafeManagement.Infrastructure.Persistence;
using CafeManagement.Infrastructure.Realtime;
using CafeManagement.Infrastructure.Security;
using CafeManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CafeManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCafeManagementInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var connectionString = configuration.GetConnectionString("CafeManagement")
            ?? "Server=.\\SQLEXPRESS;Database=CafeManagement;User Id=CafeManagementAdmin;Password=sa@CafeManagement!;TrustServerCertificate=True;MultipleActiveResultSets=True";
        services.AddDbContext<CafeManagementDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
                sqlOptions.MigrationsAssembly(typeof(CafeManagementDbContext).Assembly.FullName)));

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
        services.AddScoped<IPasswordHasher<CafeManagement.Core.Entities.AdminUser>, PasswordHasher<CafeManagement.Core.Entities.AdminUser>>();
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
