using FluentValidation;
using Gml.Web.Api.Core.Validation;
using Gml.Web.Api.Domains.Texture;
using Gml.Web.Api.Dto.Integration;
using Gml.Web.Api.Dto.Profile;
using Gml.Web.Api.Dto.User;

namespace Gml.Web.Api.Core.Extensions;

public static class ValidatorsExtensions
{
    public static IServiceCollection RegisterValidators(this IServiceCollection serviceCollection)
    {
        serviceCollection
            // Add auth validators
            .AddScoped<IValidator<UserCreateDto>, UserCreateValidationFilter>()
            .AddScoped<IValidator<UserAuthDto>, UserAuthValidationFilter>()

            // Profiles validator
            .AddScoped<IValidator<ProfileCreateDto>, ProfileCreateDtoValidator>()
            .AddScoped<IValidator<ProfileUpdateDto>, ProfileUpdateDtoValidator>()
            .AddScoped<IValidator<ProfileRestoreDto>, ProfileRestoreDtoValidator>()
            .AddScoped<IValidator<CompileProfileDto>, CompileProfileDtoValidator>()
            .AddScoped<IValidator<ProfileCreateInfoDto>, ProfileCreateInfoDtoValidator>()

            // Players validator
            .AddScoped<IValidator<BaseUserPassword>, PlayerAuthDtoValidator>()

            // Integration validator
            .AddScoped<IValidator<IntegrationUpdateDto>, IntegrationValidator>()

            // Texture validator
            .AddScoped<IValidator<TextureServiceDto>, TextureServiceDtoValidator>();

        return serviceCollection;
    }
}
