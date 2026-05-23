using CafeOrders.Infrastructure;
using CafeOrders.Infrastructure.Persistence;
using CafeOrders.Infrastructure.Realtime;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var adminCookieDays = builder.Configuration.GetValue<int?>("SessionSettings:AdminCookieDays") ?? 3650;
var slidingExpiration = builder.Configuration.GetValue<bool?>("SessionSettings:SlidingExpiration") ?? true;
var adminCookieLifetime = TimeSpan.FromDays(Math.Max(adminCookieDays, 1));

builder.Services.AddCafeOrdersInfrastructure(builder.Configuration);
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
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CafeOrdersDbContext>();
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
