using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CafeOrders.Infrastructure.Logging;

public static class HttpActivityLoggingExtensions
{
    public static IApplicationBuilder UseCafeOrdersHttpActivityLogging(this IApplicationBuilder app, string source)
    {
        return app.Use(async (context, next) =>
        {
            var stopwatch = Stopwatch.StartNew();
            Exception? exception = null;

            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                stopwatch.Stop();

                if (ShouldLog(context, exception))
                {
                    var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger($"CafeOrders.{source}.HttpActivity");
                    var endpoint = context.GetEndpoint()?.DisplayName ?? "(no endpoint)";
                    var user = ResolveUser(context);
                    var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "(unknown)";
                    var statusCode = exception is null ? context.Response.StatusCode : 500;

                    if (exception is null)
                    {
                        logger.LogInformation(
                            "{Source} HTTP activity. Method={Method}, Path={Path}, StatusCode={StatusCode}, ElapsedMs={ElapsedMs}, User={User}, RemoteIp={RemoteIp}, Endpoint={Endpoint}",
                            source,
                            context.Request.Method,
                            context.Request.Path.Value,
                            statusCode,
                            stopwatch.ElapsedMilliseconds,
                            user,
                            remoteIp,
                            endpoint);
                    }
                    else
                    {
                        logger.LogError(
                            exception,
                            "{Source} HTTP activity failed. Method={Method}, Path={Path}, StatusCode={StatusCode}, ElapsedMs={ElapsedMs}, User={User}, RemoteIp={RemoteIp}, Endpoint={Endpoint}",
                            source,
                            context.Request.Method,
                            context.Request.Path.Value,
                            statusCode,
                            stopwatch.ElapsedMilliseconds,
                            user,
                            remoteIp,
                            endpoint);
                    }
                }
            }
        });
    }

    private static bool ShouldLog(HttpContext context, Exception? exception)
    {
        if (exception is not null || context.Response.StatusCode >= 400)
        {
            return !IsNoisyPath(context.Request.Path);
        }

        if (!HttpMethods.IsPost(context.Request.Method)
            && !HttpMethods.IsPut(context.Request.Method)
            && !HttpMethods.IsDelete(context.Request.Method)
            && !HttpMethods.IsPatch(context.Request.Method))
        {
            return false;
        }

        return !IsNoisyPath(context.Request.Path);
    }

    private static bool IsNoisyPath(PathString path)
        => path.StartsWithSegments("/api/v1/devices/heartbeat", StringComparison.OrdinalIgnoreCase)
           || path.StartsWithSegments("/api/v1/logs/client", StringComparison.OrdinalIgnoreCase)
           || path.StartsWithSegments("/dashboard/live", StringComparison.OrdinalIgnoreCase)
           || path.StartsWithSegments("/dashboard/presentation", StringComparison.OrdinalIgnoreCase)
           || path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase);

    private static string ResolveUser(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return "(anonymous)";
        }

        return context.User.FindFirstValue(ClaimTypes.Name)
            ?? context.User.FindFirstValue(ClaimTypes.GivenName)
            ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "(authenticated)";
    }
}
