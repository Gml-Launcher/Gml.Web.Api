using Gml.Web.Api.Core.Integrations.Auth;
using Gml.Web.Api.Domains.System;

namespace Gml.Web.Api.Core.Extensions;

public interface IAuthServiceFactory
{
    IPlatformAuthService CreateAuthService(AuthType platformKey);
}