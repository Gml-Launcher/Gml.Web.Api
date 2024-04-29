using Gml.Web.Api.Core.Extensions;
using Gml.Web.Api.Domains.System;

namespace Gml.Web.Api.Core.Integrations.Auth;

public class AuthService(IAuthServiceFactory authServiceFactory) : IAuthService
{
    public Task<bool> CheckAuth(string login, string password, AuthType authType)
    {
        var authService = authServiceFactory.CreateAuthService(authType);

        return authService.Auth(login, password);
    }
}
