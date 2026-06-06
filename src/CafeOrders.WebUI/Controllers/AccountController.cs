using System.Security.Claims;
using CafeOrders.Application.Abstractions;
using CafeOrders.Application.Contracts.Auth;
using CafeOrders.WebUI.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CafeOrders.WebUI.Controllers;

[AllowAnonymous]
public sealed class AccountController(
    IAdminAuthService adminAuthService,
    IConfiguration configuration,
    ILogger<AccountController> logger) : Controller
{
    [HttpGet("/account/login")]
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost("/account/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, [FromQuery] string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("Admin login validation failed. UserName={UserName}, RemoteIp={RemoteIp}", model.UserName, ResolveRemoteIp());
            return View(model);
        }

        var user = await adminAuthService.ValidateCredentialsAsync(new AdminLoginRequest(model.UserName, model.Password), cancellationToken);
        if (user is null)
        {
            logger.LogWarning("Admin login failed. UserName={UserName}, RemoteIp={RemoteIp}", model.UserName, ResolveRemoteIp());
            model.ErrorMessage = "Kullanici adi veya sifre hatali.";
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.GivenName, user.UserName),
            new(ClaimTypes.Role, "Administrator")
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        var adminCookieDays = configuration.GetValue<int?>("SessionSettings:AdminCookieDays") ?? 3650;
        var cookieLifetime = TimeSpan.FromDays(Math.Max(adminCookieDays, 1));
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                IssuedUtc = DateTimeOffset.UtcNow,
                ExpiresUtc = DateTimeOffset.UtcNow.Add(cookieLifetime)
            });
        await adminAuthService.RecordLoginAsync(user.Id, cancellationToken);
        logger.LogInformation("Admin login succeeded. UserId={UserId}, UserName={UserName}, DisplayName={DisplayName}, RemoteIp={RemoteIp}", user.Id, user.UserName, user.DisplayName, ResolveRemoteIp());

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [Authorize]
    [HttpPost("/account/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        logger.LogInformation("Admin logout. User={User}, RemoteIp={RemoteIp}", User.Identity?.Name ?? "(unknown)", ResolveRemoteIp());
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private string ResolveRemoteIp()
        => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "(unknown)";
}
