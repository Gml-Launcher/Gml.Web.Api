using System.Net;
using AutoMapper;
using FluentValidation;
using Gml.Core.Launcher;
using Gml.Core.User;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Domains.System;
using Gml.Web.Api.Dto.Messages;
using Gml.Web.Api.Dto.Profile;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gml.Web.Api.Core.Handlers;

public class ProfileHandler : IProfileHandler
{
    public static async Task<IResult> GetProfiles(
        IMapper mapper,
        IGmlManager gmlManager)
    {
        var profiles = await gmlManager.Profiles.GetProfiles();

        return Results.Ok(ResponseMessage.Create(mapper.Map<IEnumerable<ProfileReadDto>>(profiles), string.Empty,
            HttpStatusCode.OK));
    }

    [Authorize]
    public static async Task<IResult> CreateProfile(
        HttpContext context,
        ISystemService systemService,
        IMapper mapper,
        IGmlManager gmlManager,
        IValidator<ProfileCreateDto> validator)
    {
        try
        {
            if (!Enum.TryParse<GameLoader>(context.Request.Form["GameLoader"], out var gameLoader))
                return Results.BadRequest(ResponseMessage.Create("Не удалось определить вид загрузчика профиля",
                    HttpStatusCode.BadRequest));

            var createDto = new ProfileCreateDto
            {
                Name = context.Request.Form["Name"],
                Description = context.Request.Form["Description"],
                Version = context.Request.Form["Version"],
                GameLoader = gameLoader
            };

            var result = await validator.ValidateAsync(createDto);

            if (!result.IsValid)
                return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                    HttpStatusCode.BadRequest));

            var checkProfile = await gmlManager.Profiles.GetProfile(createDto.Name);

            if (checkProfile is not null)
                return Results.BadRequest(ResponseMessage.Create("Профиль с данным именем уже существует",
                    HttpStatusCode.BadRequest));

            if (!await gmlManager.Profiles.CanAddProfile(createDto.Name, createDto.Version))
                return Results.BadRequest(ResponseMessage.Create("Невозможно создать профиль по полученным данным",
                    HttpStatusCode.BadRequest));

            if (context.Request.Form.Files.FirstOrDefault() is { } formFile)
                createDto.IconBase64 = await systemService.GetBase64FromImageFile(formFile);

            var profile = await gmlManager.Profiles.AddProfile(createDto.Name, createDto.Version, createDto.GameLoader,
                createDto.IconBase64, createDto.Description);

            return Results.Created($"/api/v1/profiles/{createDto.Name}",
                ResponseMessage.Create(mapper.Map<ProfileReadDto>(profile), "Профиль успешно создан",
                    HttpStatusCode.Created));
        }
        catch (Exception exception)
        {
            return Results.BadRequest(ResponseMessage.Create(exception.Message,
                HttpStatusCode.BadRequest));
        }
    }


    [Authorize]
    public static async Task<IResult> UpdateProfile(
        HttpContext context,
        ISystemService systemService,
        IMapper mapper,
        IGmlManager gmlManager,
        IValidator<ProfileUpdateDto> validator)
    {
        var updateDto = new ProfileUpdateDto
        {
            Name = context.Request.Form["Name"],
            Description = context.Request.Form["Description"],
            OriginalName = context.Request.Form["OriginalName"]
        };

        var result = await validator.ValidateAsync(updateDto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        var profile = await gmlManager.Profiles.GetProfile(updateDto.OriginalName);

        if (profile is null)
            return Results.NotFound(ResponseMessage.Create("Профиль не найден", HttpStatusCode.NotFound));

        if (updateDto.OriginalName != updateDto.Name)
        {
            var profileExists = await gmlManager.Profiles.GetProfile(updateDto.Name);

            if (profileExists != null)
                return Results.NotFound(ResponseMessage.Create("Профиль с таким наименованием уже существует",
                    HttpStatusCode.NotFound));
        }

        var icon = context.Request.Form.Files["icon"] is null
            ? null
            : context.Request.Form.Files["icon"]!.OpenReadStream();

        var background = context.Request.Form.Files["icon"] is null
            ? null
            : context.Request.Form.Files["background"]!.OpenReadStream();

        await gmlManager.Profiles.UpdateProfile(
            profile,
            updateDto.Name,
            icon,
            background,
            updateDto.Description);

        var newProfile = mapper.Map<ProfileReadDto>(profile);
        newProfile.Background = $"{context.Request.Scheme}://{context.Request.Host}/api/v1/file/{profile.BackgroundImageKey}";

        return Results.Ok(ResponseMessage.Create(newProfile, "Профиль успешно обновлен",
            HttpStatusCode.OK));
    }


    [Authorize]
    public static async Task<IResult> RestoreProfile(
        IMapper mapper,
        IGmlManager gmlManager,
        IValidator<ProfileRestoreDto> validator,
        ProfileRestoreDto restoreDto)
    {
        var result = await validator.ValidateAsync(restoreDto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        if (!Enum.TryParse(restoreDto.OsType, out OsType osType))
            return Results.BadRequest(ResponseMessage.Create("Не удалось определить вид оперционной системы профиля",
                HttpStatusCode.BadRequest));

        if (!gmlManager.Profiles.CanUpdateAndRestore)
            return Results.NotFound(ResponseMessage.Create(
                "В данный момент происходит загрузка другого профиля, восстановление и компиляция профилей недоступна",
                HttpStatusCode.NotFound));

        var profile = await gmlManager.Profiles.GetProfile(restoreDto.Name);

        if (profile is null)
            return Results.NotFound(ResponseMessage.Create("Профиль не найден", HttpStatusCode.NotFound));

        await gmlManager.Profiles.RestoreProfileInfo(profile.Name, new StartupOptions
        {
            OsType = osType,
            OsArch = restoreDto.OsArchitecture
        }, User.Empty);

        return Results.Ok(ResponseMessage.Create("Профиль успешно восстановлен", HttpStatusCode.OK));
    }

    [Authorize]
    public static async Task<IResult> CompileProfile(
        IMapper mapper,
        IGmlManager gmlManager,
        IValidator<ProfileCompileDto> validator,
        ProfileCompileDto profileDto)
    {
        var result = await validator.ValidateAsync(profileDto);
        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        if (!gmlManager.Profiles.CanUpdateAndRestore)
            return Results.NotFound(ResponseMessage.Create(
                "В данный момент происходит загрузка другого профиля, восстановление и компиляция профилей недоступна",
                HttpStatusCode.NotFound));

        var profile = await gmlManager.Profiles.GetProfile(profileDto.Name);

        if (profile is null)
            return Results.NotFound(ResponseMessage.Create("Профиль не найден", HttpStatusCode.NotFound));

        await gmlManager.Profiles.PackProfile(profile);

        return Results.Ok(ResponseMessage.Create("Профиль успешно скомпилирован", HttpStatusCode.OK));
    }

    public static async Task<IResult> GetProfileInfo(
        HttpContext context,
        IMapper mapper,
        IGmlManager gmlManager,
        IValidator<ProfileCreateInfoDto> validator,
        ProfileCreateInfoDto createInfoDto)
    {
        var result = await validator.ValidateAsync(createInfoDto);

        if (!result.IsValid)
            return Results.BadRequest(
                ResponseMessage.Create(result.Errors, "Ошибка валидации", HttpStatusCode.BadRequest));


        if (!Enum.TryParse(createInfoDto.OsType, out OsType osType))
            return Results.BadRequest(ResponseMessage.Create("Не удалось определить вид оперционной системы профиля",
                HttpStatusCode.BadRequest));

        var profile = await gmlManager.Profiles.GetProfile(createInfoDto.ProfileName);

        if (profile is null)
            return Results.NotFound(ResponseMessage.Create($"Профиль \"{createInfoDto.ProfileName}\" не найден",
                HttpStatusCode.NotFound));

        var profileInfo = await gmlManager.Profiles.GetProfileInfo(profile.Name, new StartupOptions
        {
            FullScreen = createInfoDto.IsFullScreen,
            ServerIp = createInfoDto.GameAddress,
            ServerPort = createInfoDto.GamePort,
            ScreenHeight = createInfoDto.WindowHeight,
            ScreenWidth = createInfoDto.WindowWidth,
            MaximumRamMb = createInfoDto.RamSize,
            MinimumRamMb = createInfoDto.RamSize,
            OsType = osType
        }, new User
        {
            Name = createInfoDto.UserName,
            Uuid = createInfoDto.UserUuid,
            AccessToken = createInfoDto.UserAccessToken
        });

        var profileDto = mapper.Map<ProfileReadInfoDto>(profileInfo);

        profileDto.Background = $"{context.Request.Scheme}://{context.Request.Host}/api/v1/file/{profile.BackgroundImageKey}";

        return Results.Ok(ResponseMessage.Create(profileDto, string.Empty, HttpStatusCode.OK));
    }

    [Authorize]
    public static async Task<IResult> RemoveProfile(
        IGmlManager gmlManager,
        string profileNames,
        [FromQuery] bool removeFiles)
    {
        var profileNamesList = profileNames.Split(',');
        var notRemovedProfiles = new List<string>();

        foreach (var profileName in profileNamesList)
        {
            var profile = await gmlManager.Profiles.GetProfile(profileName);

            if (profile == null)
                notRemovedProfiles.Add(profileName);
            else
                await gmlManager.Profiles.RemoveProfile(profile, removeFiles);
        }

        var message = "Операция выполнена";

        if (notRemovedProfiles.Any())
        {
            message += ". Было пропущено удаление:";
            message += string.Join(",", notRemovedProfiles);
        }

        return Results.Ok(ResponseMessage.Create(message, HttpStatusCode.OK));
    }
}
