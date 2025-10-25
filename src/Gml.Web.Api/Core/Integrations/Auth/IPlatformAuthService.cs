using Gml.Domains.Integrations;

namespace Gml.Web.Api.Core.Integrations.Auth;

public interface IPlatformAuthService
{
    Task<AuthResult> Auth(string login, string password, string? totp = null);
}
