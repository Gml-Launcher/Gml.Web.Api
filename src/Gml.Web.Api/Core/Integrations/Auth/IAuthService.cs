using Gml.Web.Api.Domains.System;

namespace Gml.Web.Api.Core.Integrations.Auth;

public interface IAuthService
{
    Task<bool> CheckAuth(string login, string password, AuthType authType);
}