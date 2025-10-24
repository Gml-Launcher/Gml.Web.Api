using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Gml.Domains.Repositories;
using GmlCore.Interfaces.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Gml.Web.Api.Core.Authentication;

public class ExternalApplicationAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IExternalApplicationRepository _externalAppRepository;
    private readonly IAccessTokenService _tokenService;

    public ExternalApplicationAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IExternalApplicationRepository externalAppRepository,
        IAccessTokenService tokenService)
        : base(options, logger, encoder)
    {
        _externalAppRepository = externalAppRepository;
        _tokenService = tokenService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return AuthenticateResult.NoResult();
        }

        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = authHeader.Substring("Bearer ".Length).Trim();
        if (string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.Fail("Invalid token");
        }

        // Hash the token and search in database
        var tokenHash = _tokenService.HashRefreshToken(token);
        var externalApp = await _externalAppRepository.GetByTokenHashAsync(tokenHash);

        if (externalApp == null)
        {
            return AuthenticateResult.Fail("Invalid application token");
        }

        // Create claims identity
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, externalApp.UserId.ToString()),
            new("sub", externalApp.UserId.ToString()),
            new("app_id", externalApp.Id.ToString()),
            new("app_name", externalApp.Name)
        };

        // Add permissions from application
        foreach (var appPerm in externalApp.ApplicationPermissions)
        {
            claims.Add(new Claim("perm", appPerm.Permission.Name));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
