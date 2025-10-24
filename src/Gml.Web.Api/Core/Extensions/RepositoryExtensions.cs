using Gml.Domains.Repositories;
using Gml.Web.Api.Core.Repositories;

namespace Gml.Web.Api.Core.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection RegisterRepositories(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IUserRepository, UserRepository>();
        serviceCollection.AddScoped<ISettingsRepository, SettingsRepository>();
        serviceCollection.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        serviceCollection.AddScoped<IRbacRepository, RbacRepository>();
        serviceCollection.AddScoped<IExternalApplicationRepository, ExternalApplicationRepository>();

        return serviceCollection;
    }
}
