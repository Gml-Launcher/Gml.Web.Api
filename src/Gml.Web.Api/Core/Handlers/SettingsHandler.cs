using System.Net;
using AutoMapper;
using FluentValidation;
using Gml.Domains.Repositories;
using Gml.Domains.Settings;
using Gml.Dto.Auth;
using Gml.Dto.Messages;
using Gml.Dto.Settings;
using Gml.Dto.User;
using Gml.Web.Api.Core.Options;
using Gml.Web.Api.Core.Repositories;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Data;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Auth;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Gml.Web.Api.Core.Handlers;

public abstract class SettingsHandler : ISettingsHandler
{
    public static async Task<IResult> UpdateSettings(
        ISettingsRepository settingsService,
        IMapper mapper,
        SettingsUpdateDto settingsDto)
    {
        var settings = mapper.Map<Settings>(settingsDto);

        var result = await settingsService.UpdateSettings(settings);

        return Results.Ok(ResponseMessage.Create(
            mapper.Map<SettingsReadDto>(result),
            string.Empty,
            HttpStatusCode.OK));
    }

    public static async Task<IResult> GetSettings(ISettingsRepository settingsService, IMapper mapper)
    {
        var settings = await settingsService.GetSettings();

        return Results.Ok(ResponseMessage.Create(
            mapper.Map<SettingsReadDto>(settings),
            "Настройки получены",
            HttpStatusCode.OK));
    }

    public static async Task<IResult> Install(
        HttpContext context,
        IGmlManager gmlManager,
        ISettingsRepository settingsService,
        IUserRepository userRepository,
        IMapper mapper,
        IValidator<SettingsInstallRecord> validator,
        HttpContext httpContext,
        IValidator<UserCreateDto> userValidator,
        ApplicationContext appContext,
        IAccessTokenService tokenService,
        IRefreshTokenRepository refreshRepo,
        DatabaseContext db,
        ServerSettings serverSettings,
        [FromBody] SettingsInstallRecord dto)
    {
        var settings = await settingsService.GetSettings();

        if (settings is null || settings.IsInstalled)
        {
            return Results.Forbid();
        }

        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return Results.BadRequest(ResponseMessage.Create(validationResult.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        var result = await AuthHandler.CreateUser(httpContext, userRepository, userValidator, mapper, new UserCreateDto
        {
            Email = dto.AdminEmail,
            Login = dto.AdminUsername,
            Password = dto.AdminPassword
        }, appContext, tokenService, refreshRepo, serverSettings, db);

        try
        {
            switch (result)
            {
                case BadRequest<object>:
                    return result;
                case Ok<ResponseMessage<AuthTokensDto>>:
                    settings.IsInstalled = true;
                    settings.ProjectName = dto.ProjectName;
                    var address = new Uri(dto.BackendAddress);
                    await gmlManager.Integrations.SetSentryService($"{address.Scheme}://gml@{address.Authority}/1");

                    await settingsService.UpdateSettings(settings);

                    return result;
                default:
                    return result;
            }
        }
        catch (Exception e)
        {
            gmlManager.BugTracker.CaptureException(e);
        }

        return Results.BadRequest();
    }

    public static async Task<IResult> Restore(
        HttpContext context,
        RestoreService restoreService,
        ISettingsRepository settingsService,
        string backupKey)
    {
        var settings = await settingsService.GetSettings();

        if (settings is null || settings.IsInstalled)
        {
            return Results.Forbid();
        }

        try
        {
            restoreService.Restore(backupKey);

            return Results.Ok();
        }
        catch (Exception e)
        {
            return Results.BadRequest(ResponseMessage.Create(e.Message, HttpStatusCode.BadRequest));
        }
    }

    public static async Task<IResult> GetKeys(
        HttpContext context,
        RestoreService restoreService,
        ISettingsRepository settingsService)
    {
        var settings = await settingsService.GetSettings();

        if (settings is null || settings.IsInstalled)
        {
            return Results.Forbid();
        }

        var keys = await restoreService.GetBackupKeysAsync();

        return Results.Ok(ResponseMessage.Create(keys, "Ошибка валидации",
            HttpStatusCode.OK));
    }

    public static async Task<IResult> IsNotInstalled(
        ISettingsRepository settingsService)
    {
        var settings = await settingsService.GetSettings();

        if (settings is null || settings.IsInstalled)
        {
            return Results.Forbid();
        }

        return Results.Ok();
    }

    public record SettingsInstallRecord(
        string ProjectName,
        string BackendAddress,
        string AdminUsername,
        string AdminEmail,
        string AdminPassword,
        string ConfirmPassword);
}
