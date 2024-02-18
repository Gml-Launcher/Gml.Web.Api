using System.Net;
using AutoMapper;
using FluentValidation;
using Gml.Core.Launcher;
using Gml.Core.User;
using Gml.Web.Api.Core.Messages;
using Gml.Web.Api.Domains.System;
using Gml.Web.Api.Dto.Profile;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Enums;
using Microsoft.AspNetCore.Authorization;

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
        IMapper mapper,
        IGmlManager gmlManager,
        IValidator<ProfileCreateDto> validator,
        ProfileCreateDto createDto)
    {
        var result = await validator.ValidateAsync(createDto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        if (!Enum.TryParse(createDto.GameLoader, out GameLoader gameLoader))
            return Results.BadRequest(ResponseMessage.Create("Не удалось определить вид загрузчика профиля",
                HttpStatusCode.BadRequest));


        var checkProfile = await gmlManager.Profiles.GetProfile(createDto.Name);

        if (checkProfile is not null)
            return Results.BadRequest(ResponseMessage.Create("Профиль с данным именем уже существует",
                HttpStatusCode.BadRequest));

        if (!await gmlManager.Profiles.CanAddProfile(createDto.Name, createDto.Version))
            return Results.BadRequest(ResponseMessage.Create("Невозможно создать профиль по полученным данным",
                HttpStatusCode.BadRequest));

        var profile = await gmlManager.Profiles.AddProfile(createDto.Name, createDto.Version, gameLoader,
            createDto.IconBase64, createDto.Description);

        return Results.Created($"/api/v1/profiles/{createDto.Name}",
            ResponseMessage.Create(mapper.Map<ProfileReadDto>(profile), "Профиль успешно создан",
                HttpStatusCode.Created));
    }


    [Authorize]
    public static async Task<IResult> UpdateProfile(
        IMapper mapper,
        IGmlManager gmlManager,
        IValidator<ProfileUpdateDto> validator,
        ProfileUpdateDto updateDto)
    {
        var result = await validator.ValidateAsync(updateDto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));
        var profile = await gmlManager.Profiles.GetProfile(updateDto.OriginalName);

        if (profile is null)
            return Results.NotFound(ResponseMessage.Create("Профиль не найден", HttpStatusCode.NotFound));

        await gmlManager.Profiles.UpdateProfile(profile, updateDto.Name, updateDto.IconBase64, updateDto.Description);

        return Results.Ok(ResponseMessage.Create(mapper.Map<ProfileReadDto>(profile), "Профиль успешно обновлен",
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

        var profile = await gmlManager.Profiles.GetProfile(restoreDto.Name);

        if (profile is null)
            return Results.NotFound(ResponseMessage.Create("Профиль не найден", HttpStatusCode.NotFound));

        await gmlManager.Profiles.RestoreProfileInfo(profile.Name, new StartupOptions
        {
            OsType = osType
        }, User.Empty);

        return Results.Ok(ResponseMessage.Create("Профиль успешно восстановлен", HttpStatusCode.OK));
    }

    [Authorize]
    public static async Task<IResult> CompileProfile(
        IMapper mapper,
        IGmlManager gmlManager,
        IValidator<CompileProfileDto> validator,
        CompileProfileDto profileDto)
    {
        var result = await validator.ValidateAsync(profileDto);
        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        var profile = await gmlManager.Profiles.GetProfile(profileDto.Name);

        if (profile is null)
            return Results.NotFound(ResponseMessage.Create("Профиль не найден", HttpStatusCode.NotFound));

        await gmlManager.Profiles.PackProfile(profile);

        return Results.Ok(ResponseMessage.Create("Профиль успешно скомпилирован", HttpStatusCode.OK));
    }

    public static async Task<IResult> GetProfileInfo(
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

        return Results.Ok(ResponseMessage.Create(mapper.Map<ProfileReadInfoDto>(profileInfo), string.Empty,
            HttpStatusCode.OK));
    }

    [Authorize]
    public static async Task<IResult> RemoveProfile(
        IGmlManager gmlManager,
        string profileName)
    {
        var profile = await gmlManager.Profiles.GetProfile(profileName);

        if (profile is null)
            return Results.NotFound(ResponseMessage.Create($"Профиль \"{profileName}\" не найден",
                HttpStatusCode.NotFound));

        await gmlManager.Profiles.RemoveProfile(profile);

        return Results.Ok(ResponseMessage.Create("Профиль был успешно удалён", HttpStatusCode.OK));
    }
}