namespace Gml.Web.Api.Core.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection RegisterCors(this IServiceCollection serviceCollection, string policyName)
    {
        serviceCollection
            .AddCors(o => o.AddPolicy(policyName, policyBuilder =>
            {
                policyBuilder.WithOrigins("http://localhost:3001")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();

            }));

        return serviceCollection;
    }
}
