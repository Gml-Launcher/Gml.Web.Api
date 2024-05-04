using Gml.Web.Api.Core.Extensions;
using Gml.Web.Api.Domains.System;

namespace Gml.Web.Api.Core.Integrations.Auth;

public class AuthServiceFactory(IServiceProvider serviceProvider) : IAuthServiceFactory
{
    public IPlatformAuthService CreateAuthService(AuthType platformKey)
    {
        return platformKey switch
        {
            AuthType.Undefined => serviceProvider.GetRequiredService<UndefinedAuthService>(),
            AuthType.DataLifeEngine => serviceProvider.GetRequiredService<DataLifeEngineAuthService>(),
            AuthType.Any => serviceProvider.GetRequiredService<AnyAuthService>(),
            AuthType.Azuriom => serviceProvider.GetRequiredService<AzuriomAuthService>(),
            _ => throw new ArgumentOutOfRangeException(nameof(platformKey), platformKey, null)
        };
    }
}
