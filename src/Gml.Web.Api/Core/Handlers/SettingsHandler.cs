using System.Net;
using AutoMapper;
using FluentValidation;
using Gml.Web.Api.Core.Repositories;
using Gml.Web.Api.Core.Validation;
using Gml.Web.Api.Domains.Settings;
using Gml.Web.Api.Dto.Messages;
using Gml.Web.Api.Dto.Settings;
using GmlCore.Interfaces;
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
        IGmlManager gmlManager,
        ISettingsRepository settingsService,
        IMapper mapper,
        IValidator<SettingsInstallRecord> validator,
        [FromBody] SettingsInstallRecord dto)
    {
        var settings = await settingsService.GetSettings();

        if (settings is not null && settings.IsInstalled)
        {
            return Results.Forbid();
        }

        var result = await validator.ValidateAsync(dto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        return Results.Ok(ResponseMessage.Create(
            mapper.Map<SettingsReadDto>(settings),
            "Настройки получены",
            HttpStatusCode.OK));
    }

    public record SettingsInstallRecord(
        string ProjectName,
        string BackendAddress,
        string AdminUsername,
        string AdminPassword,
        string ConfirmPassword);
}
