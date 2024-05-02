using FluentValidation;
using Gml.Web.Api.Core.Validation;
using Gml.Web.Api.Dto.Files;
using Gml.Web.Api.Dto.Integration;
using Gml.Web.Api.Dto.Launcher;
using Gml.Web.Api.Dto.Profile;
using Gml.Web.Api.Dto.Texture;
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
            .AddScoped<IValidator<ProfileCompileDto>, CompileProfileDtoValidator>()
            .AddScoped<IValidator<ProfileCreateInfoDto>, ProfileCreateInfoDtoValidator>()

            // Players validator
            .AddScoped<IValidator<BaseUserPassword>, PlayerAuthDtoValidator>()

            // Integration validator
            .AddScoped<IValidator<IntegrationUpdateDto>, IntegrationValidator>()

            // Files validator
            .AddScoped<IValidator<FileWhiteListDto>, FileWhiteListValidator>()

            // Launcher validator
            .AddScoped<IValidator<LauncherCreateDto>, LauncherCreateDtoValidator>()

            // Texture validator
            .AddScoped<IValidator<UrlServiceDto>, TextureServiceDtoValidator>();

        return serviceCollection;
    }
}
