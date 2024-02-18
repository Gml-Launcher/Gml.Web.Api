using Gml.Web.Api.Core.Extensions;
using Gml.Web.Api.Domains.System;

namespace Gml.Web.Api.Core.Integrations.Auth;

public class AuthServiceFactory(IServiceProvider serviceProvider) : IAuthServiceFactory
{
    public IPlatformAuthService CreateAuthService(AuthType platformKey)
    {
        switch (platformKey)
        {
            case AuthType.Undefined:
                return serviceProvider.GetRequiredService<UndefinedAuthService>();
            case AuthType.DataLifeEngine:
                return serviceProvider.GetRequiredService<DataLifeEngineAuthService>();
            default:
                throw new ArgumentOutOfRangeException(nameof(platformKey), platformKey, null);
        }

        throw new ArgumentOutOfRangeException(nameof(platformKey), platformKey, null);
    }
}