using Gml.Web.Api.Domains.System;
using GmlCore.Interfaces.Enums;

namespace Gml.Web.Api.Core.Integrations.Auth;

public interface IAuthService
{
    Task<bool> CheckAuth(string login, string password, AuthType authType);
}
