namespace Gml.Web.Api.Core.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection RegisterCors(this IServiceCollection serviceCollection, string policyName)
    {
        serviceCollection
            .AddCors(o => o.AddPolicy(policyName, policyBuilder =>
            {
                policyBuilder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            }));

        return serviceCollection;
    }
}
