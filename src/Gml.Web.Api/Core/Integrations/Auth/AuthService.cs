using Gml.Web.Api.Core.Extensions;
using Gml.Web.Api.Domains.Integrations;
using Gml.Web.Api.Domains.System;
using GmlCore.Interfaces.Enums;

namespace Gml.Web.Api.Core.Integrations.Auth;

public class AuthService(IAuthServiceFactory authServiceFactory) : IAuthService
{
    public Task<AuthResult> CheckAuth(string login, string password, AuthType authType)
    {
        var authService = authServiceFactory.CreateAuthService(authType);

        return authService.Auth(login, password);
    }
}
