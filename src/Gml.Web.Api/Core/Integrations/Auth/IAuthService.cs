using Gml.Web.Api.Domains.Integrations;
using GmlCore.Interfaces.Enums;

namespace Gml.Web.Api.Core.Integrations.Auth;

public interface IAuthService
{
    Task<AuthResult> CheckAuth(string login, string password, AuthType authType);
}
