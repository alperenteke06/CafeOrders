namespace CafeOrders.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "CafeOrders.API";
    public string Audience { get; set; } = "CafeOrders.DesktopApps";
    public string Key { get; set; } = "super-secret-demo-key-change-this";
    public int ExpiryMinutes { get; set; } = 720;
}
