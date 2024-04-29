using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;

namespace Gml.Web.Api.Core.Extensions;

public static class RegisterSwaggerExtensions
{
    public static IServiceCollection RegisterSwagger(this IServiceCollection serviceCollection, string projectName,
        string? projectDescription)
    {
        serviceCollection.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = projectName,
                Version = "v1",
                Description = projectDescription
            });

            var securitySchema = new OpenApiSecurityScheme
            {
                Description =
                    "В проекте используется JWT авторизация, для выполнения некоторых запросов необходим AccessToken. Вставьте его в поле ниже",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                BearerFormat = "JWT"
            };

            c.AddSecurityDefinition("Bearer", securitySchema);
            var securityRequirement = new OpenApiSecurityRequirement { { securitySchema, new[] { "Bearer" } } };
            c.AddSecurityRequirement(securityRequirement);
        });

        return serviceCollection;
    }
}
