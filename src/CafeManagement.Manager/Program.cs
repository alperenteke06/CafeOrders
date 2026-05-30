using CafeManagement.Infrastructure;
using CafeManagement.Infrastructure.Persistence;
using CafeManagement.Infrastructure.Realtime;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var adminCookieDays = builder.Configuration.GetValue<int?>("SessionSettings:AdminCookieDays") ?? 3650;
var slidingExpiration = builder.Configuration.GetValue<bool?>("SessionSettings:SlidingExpiration") ?? true;
var adminCookieLifetime = TimeSpan.FromDays(Math.Max(adminCookieDays, 1));
var dataProtectionApplicationName = builder.Configuration["SessionSettings:DataProtectionApplicationName"] ?? "CafeManagement.Manager";
var dataProtectionKeysPath = builder.Configuration["SessionSettings:DataProtectionKeysPath"];

builder.Services.AddCafeManagementInfrastructure(builder.Configuration);
var dataProtectionBuilder = builder.Services.AddDataProtection()
    .SetApplicationName(dataProtectionApplicationName);
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    var expandedKeysPath = Environment.ExpandEnvironmentVariables(dataProtectionKeysPath);
    Directory.CreateDirectory(expandedKeysPath);
    dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(expandedKeysPath));
}

builder.Services.AddHttpClient();
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.AccessDeniedPath = "/account/login";
        options.SlidingExpiration = slidingExpiration;
        options.ExpireTimeSpan = adminCookieLifetime;
        options.Cookie.MaxAge = options.ExpireTimeSpan;
        options.Cookie.Name = "CafeManagement.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CafeManagementDbContext>();
    await dbContext.Database.MigrateAsync();
    await DbSeeder.SeedAsync(dbContext);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<CafeHub>("/hubs/cafe");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
