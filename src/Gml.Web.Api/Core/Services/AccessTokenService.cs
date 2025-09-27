using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Gml.Web.Api.Core.Options;
using GmlCore.Interfaces.Auth;
using Microsoft.IdentityModel.Tokens;

namespace Gml.Web.Api.Core.Services;

public class AccessTokenService : IAccessTokenService
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

    public string GenerateAccessToken(int userId, string userLogin, string userEmail, IEnumerable<string> roles,
        IEnumerable<string> permissions)
        => GenerateAccessToken(userId.ToString(), userLogin, userEmail, roles, permissions);

    public string GenerateAccessToken(string subject, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        return GenerateAccessTokenCore(
            subject: subject,
            userLogin: null,
            userEmail: null,
            roles: roles,
            permissions: permissions
        );
    }

    public string GenerateAccessToken(string subject, string userLogin, string userEmail, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        return GenerateAccessTokenCore(
            subject: subject,
            userLogin: userLogin,
            userEmail: userEmail,
            roles: roles,
            permissions: permissions
        );
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

    // Internal helpers
    private string GenerateAccessTokenCore(
        string subject,
        string? userLogin,
        string? userEmail,
        IEnumerable<string>? roles,
        IEnumerable<string>? permissions)
    {
        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new("sub", subject),
            new(ClaimTypes.NameIdentifier, subject)
        };

        if (!string.IsNullOrWhiteSpace(userLogin))
            claims.Add(new Claim(ClaimTypes.Name, userLogin));
        if (!string.IsNullOrWhiteSpace(userEmail))
            claims.Add(new Claim(ClaimTypes.Email, userEmail));

        AddClaims(claims, ClaimTypes.Role, roles);
        AddClaims(claims, "perm", permissions);

        var token = CreateJwtToken(claims, now);
        return _handler.WriteToken(token);
    }

    private static void AddClaims(List<Claim> target, string claimType, IEnumerable<string>? values)
    {
        if (values is null) return;
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
                target.Add(new Claim(claimType, v));
        }
    }


    private JwtSecurityToken CreateJwtToken(IEnumerable<Claim> claims, DateTime now)
    {
        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        return new JwtSecurityToken(
            issuer: _settings.JwtIssuer,
            audience: _settings.JwtAudience,
            claims: claims,
            notBefore: now,
            //expires: now.AddMinutes(_settings.AccessTokenMinutes),
            expires: now.AddMinutes(1),
            signingCredentials: creds);
    }
}
