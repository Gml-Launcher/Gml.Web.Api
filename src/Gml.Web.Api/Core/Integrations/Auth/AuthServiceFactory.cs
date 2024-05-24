using Gml.Web.Api.Core.Extensions;
using Gml.Web.Api.Domains.System;
using GmlCore.Interfaces.Enums;

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
            AuthType.EasyCabinet => serviceProvider.GetRequiredService<EasyCabinetAuthService>(),
            AuthType.UnicoreCMS => serviceProvider.GetRequiredService<UnicoreCMSAuthService>(),
            _ => throw new ArgumentOutOfRangeException(nameof(platformKey), platformKey, null)
        };
    }
}
