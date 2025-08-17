using Gml.Web.Api.Core.Repositories;
using Gml.Web.Api.Domains.Repositories;

namespace Gml.Web.Api.Core.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection RegisterRepositories(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IUserRepository, UserRepository>();
        serviceCollection.AddScoped<ISettingsRepository, SettingsRepository>();

        return serviceCollection;
    }
}
