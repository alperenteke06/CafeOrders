using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CafeManagement.Application.Abstractions;
using CafeManagement.Core.Entities;
using CafeManagement.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CafeManagement.Infrastructure.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;

    public string CreateDeviceToken(Device device)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, device.Id.ToString()),
            new Claim("deviceId", device.Id.ToString()),
            new Claim("tableId", device.TableId?.ToString() ?? string.Empty),
            new Claim("deviceKey", device.DeviceKey)
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
