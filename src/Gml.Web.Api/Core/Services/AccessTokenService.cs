using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Gml.Web.Api.Core.Options;
using Microsoft.IdentityModel.Tokens;

namespace Gml.Web.Api.Core.Services;

public class AccessTokenService
{
    private readonly ServerSettings _settings;
    private readonly JwtSecurityTokenHandler _handler = new();
    private readonly SymmetricSecurityKey _key;

    public AccessTokenService(ServerSettings settings)
    {
        _settings = settings;
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecurityKey));
    }

    public string GenerateAccessToken(int userId, string? role = null)
        => GenerateAccessToken(userId.ToString(), role);

    public string GenerateAccessToken(string subject, string? role = null)
        => GenerateAccessToken(subject, role is null ? Array.Empty<string>() : new[] { role }, Array.Empty<string>());

    public string GenerateAccessToken(int userId, IEnumerable<string> roles, IEnumerable<string> permissions)
        => GenerateAccessToken(userId.ToString(), roles, permissions);

    public string GenerateAccessToken(string subject, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new("sub", subject),
            new(ClaimTypes.NameIdentifier, subject)
        };

        if (roles != null)
        {
            foreach (var r in roles)
            {
                if (!string.IsNullOrWhiteSpace(r))
                    claims.Add(new Claim(ClaimTypes.Role, r));
            }
        }

        if (permissions != null)
        {
            foreach (var p in permissions)
            {
                if (!string.IsNullOrWhiteSpace(p))
                    claims.Add(new Claim("perm", p));
            }
        }

        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _settings.JwtIssuer,
            audience: _settings.JwtAudience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_settings.AccessTokenMinutes),
            signingCredentials: creds);
        return _handler.WriteToken(token);
    }

    public bool ValidateToken(string token)
    {
        try
        {
            _handler.ValidateToken(token, new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _key,
                ValidateIssuer = true,
                ValidIssuer = _settings.JwtIssuer,
                ValidateAudience = true,
                ValidAudience = _settings.JwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string HashRefreshToken(string refreshToken)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(bytes);
    }
}
