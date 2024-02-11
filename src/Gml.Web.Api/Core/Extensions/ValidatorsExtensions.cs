using FluentValidation;
using Gml.Web.Api.Core.Validation;
using Gml.Web.Api.Dto.User;

namespace Gml.Web.Api.Core.Extensions;

public static class ValidatorsExtensions
{
    public static IServiceCollection RegisterValidators(this IServiceCollection serviceCollection)
    {
        serviceCollection
            // Add validators
            .AddScoped<IValidator<UserCreateDto>, UserCreateValidationFilter>()
            .AddScoped<IValidator<UserAuthDto>, UserAuthValidationFilter>();

        return serviceCollection;
    }
}