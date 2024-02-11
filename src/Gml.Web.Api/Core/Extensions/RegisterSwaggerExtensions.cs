namespace Gml.Web.Api.Core.Extensions;

public static class RegisterSwaggerExtensions
{
    public static IServiceCollection RegisterSwagger(this IServiceCollection serviceCollection, string projectName,
        string? projectDescription)
    {
        serviceCollection.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Title = projectName,
                Version = "v1",
                Description = projectDescription
            });
        });

        return serviceCollection;
    }
}