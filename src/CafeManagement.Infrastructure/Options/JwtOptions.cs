namespace CafeManagement.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "CafeManagement.Api";
    public string Audience { get; set; } = "CafeManagement.Kiosks";
    public string Key { get; set; } = "super-secret-demo-key-change-this";
    public int ExpiryMinutes { get; set; } = 720;
}
