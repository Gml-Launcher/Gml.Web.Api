using Mapster;
using MapsterMapper;

namespace Gml.Web.Api.Core.Extensions;

public static class MappingExtensions
{
    public static IServiceCollection AddMapping(this IServiceCollection services)
    {
        var config = new TypeAdapterConfig();
        config.Scan(typeof(Gml.Web.Api.Core.MappingProfiles.UserMapper).Assembly);
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        return services;
    }
}
