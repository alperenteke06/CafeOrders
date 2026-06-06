using System.Text.Json.Serialization;
using CafeOrders.Infrastructure;
using CafeOrders.Infrastructure.Logging;
using CafeOrders.Infrastructure.Persistence;
using CafeOrders.Infrastructure.Realtime;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var applicationLogQueue = new ApplicationLogQueue();
builder.Logging.AddLocalFile(builder.Configuration, "CafeOrders.API.log");
builder.Logging.AddApplicationLogQueue(builder.Configuration, "API", applicationLogQueue);

builder.Services.AddSingleton<IApplicationLogQueue>(applicationLogQueue);
builder.Services.AddHostedService<ApplicationLogWriterService>();
builder.Services.AddCafeOrdersInfrastructure(builder.Configuration);
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Lan", policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(_ => true);
    });
});

var app = builder.Build();
var systemLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("CafeOrders.API.System");
systemLogger.LogInformation(
    "CafeOrders API starting. Environment={Environment}, Urls={Urls}, ContentRoot={ContentRoot}",
    app.Environment.EnvironmentName,
    builder.Configuration["Urls"] ?? "(default)",
    app.Environment.ContentRootPath);

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CafeOrdersDbContext>();
    await dbContext.Database.MigrateAsync();
    await DbSeeder.SeedAsync(dbContext);
}
systemLogger.LogInformation("CafeOrders API database migration and seed completed.");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Lan");
app.UseAuthentication();
app.UseAuthorization();
app.UseCafeOrdersHttpActivityLogging("API");
app.MapControllers();
app.MapHub<CafeHub>("/hubs/cafe");

app.Run();
