using Gml.Web.Api.Core.Integrations.Auth;
using GmlCore.Interfaces.Enums;

namespace Gml.Web.Api.Core.Extensions;

public interface IAuthServiceFactory
{
    IPlatformAuthService CreateAuthService(AuthType platformKey);
}
