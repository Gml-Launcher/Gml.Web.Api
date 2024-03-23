namespace Gml.Web.Api.Core.Integrations.Auth;

public class AnyAuthService : IPlatformAuthService
{
    public Task<bool> Auth(string login, string password)
    {
        return Task.FromResult(true);
    }
}
