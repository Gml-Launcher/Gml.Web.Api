using Gml.Web.Api.Domains.Integrations;

namespace Gml.Web.Api.Core.Integrations.Auth;

public class AnyAuthService : IPlatformAuthService
{
    public Task<AuthResult> Auth(string login, string password, string? totp = null)
    {
        return Task.FromResult(new AuthResult
        {
            Login = login,
            IsSuccess = true,
            IsSlim = false
        });
    }
}
