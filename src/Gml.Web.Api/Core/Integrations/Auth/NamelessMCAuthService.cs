using Gml.Web.Api.Domains.Integrations;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Integrations.Auth;

public class NamelessMCAuthService(IHttpClientFactory httpClientFactory, IGmlManager gmlManager) : IPlatformAuthService
{
    public Task<AuthResult> Auth(string login, string password)
    {
        throw new NotImplementedException();
    }
}
