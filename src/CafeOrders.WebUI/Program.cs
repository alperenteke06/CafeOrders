using CafeOrders.Infrastructure;
using CafeOrders.Infrastructure.Logging;
using CafeOrders.Infrastructure.Persistence;
using CafeOrders.Infrastructure.Realtime;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var applicationLogQueue = new ApplicationLogQueue();
builder.Logging.AddLocalFile(builder.Configuration, "CafeOrders.WebUI.log");
builder.Logging.AddApplicationLogQueue(builder.Configuration, "WebUI", applicationLogQueue);
var adminCookieDays = builder.Configuration.GetValue<int?>("SessionSettings:AdminCookieDays") ?? 3650;
var slidingExpiration = builder.Configuration.GetValue<bool?>("SessionSettings:SlidingExpiration") ?? true;
var adminCookieLifetime = TimeSpan.FromDays(Math.Max(adminCookieDays, 1));
var dataProtectionApplicationName = builder.Configuration["SessionSettings:DataProtectionApplicationName"] ?? "CafeOrders.WebUI";
var dataProtectionKeysPath = builder.Configuration["SessionSettings:DataProtectionKeysPath"];

builder.Services.AddSingleton<IApplicationLogQueue>(applicationLogQueue);
builder.Services.AddHostedService<ApplicationLogWriterService>();
builder.Services.AddCafeOrdersInfrastructure(builder.Configuration);
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
        options.Cookie.Name = "CafeOrders.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });
builder.Services.AddAuthorization();

var app = builder.Build();
var systemLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("CafeOrders.WebUI.System");
systemLogger.LogInformation(
    "CafeOrders WebUI starting. Environment={Environment}, Urls={Urls}, ContentRoot={ContentRoot}, ApiBaseUrl={ApiBaseUrl}",
    app.Environment.EnvironmentName,
    builder.Configuration["Urls"] ?? "(default)",
    app.Environment.ContentRootPath,
    builder.Configuration["ApiBaseUrl"] ?? "(auto)");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CafeOrdersDbContext>();
    await dbContext.Database.MigrateAsync();
    await DbSeeder.SeedAsync(dbContext);
}
systemLogger.LogInformation("CafeOrders WebUI database migration and seed completed.");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseCafeOrdersHttpActivityLogging("WebUI");
app.MapHub<CafeHub>("/hubs/cafe");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
