using System.Diagnostics;
using System.Net;
using AutoMapper;
using FluentValidation;
using Gml.Common;
using Gml.Core;
using Gml.Core.Launcher;
using Gml.Core.User;
using Gml.Models.Mods;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Domains.Exceptions;
using Gml.Web.Api.Domains.System;
using Gml.Web.Api.Dto.Messages;
using Gml.Web.Api.Dto.Mods;
using Gml.Web.Api.Dto.Player;
using Gml.Web.Api.Dto.Profile;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Gml.Web.Api.Core.Handlers;

public class ProfileHandler : IProfileHandler
{
    public static async Task<IResult> GetProfiles(
        HttpContext context,
        IMapper mapper,
        IGmlManager gmlManager)
    {
        IEnumerable<IGameProfile> profiles = [];

        if (context.User.IsInRole("Player"))
        {
            var userName = context.User.Identity?.Name;

            if (string.IsNullOrEmpty(userName))
            {
                return Results.BadRequest(ResponseMessage.Create("Не удалось идентифицировать пользователя",
                    HttpStatusCode.BadRequest));
            }

            var user = await gmlManager.Users.GetUserByName(userName);

            if (user is null)
            {
                return Results.BadRequest(ResponseMessage.Create("Не удалось идентифицировать пользователя",
                    HttpStatusCode.BadRequest));
            }

            profiles = (await gmlManager.Profiles.GetProfiles())
                .Where(c =>
                    c is { IsEnabled: true, UserWhiteListGuid.Count: 0 } ||
                    c.UserWhiteListGuid.Any(g => g.Equals(user.Uuid)));

        }else if (context.User.IsInRole("Admin"))
        {
            profiles = await gmlManager.Profiles.GetProfiles();
        }

        var gameProfiles = profiles as IGameProfile[] ?? profiles.ToArray();

        var dtoProfiles = mapper.Map<ProfileReadDto[]>(profiles);

        foreach (var profile in dtoProfiles)
        {
            var originalProfile = gameProfiles.First(c => c.Name == profile.Name);
            profile.Background = $"{context.Request.Scheme}://{context.Request.Host}/api/v1/file/{originalProfile.BackgroundImageKey}";
        }

        return Results.Ok(ResponseMessage.Create(dtoProfiles, string.Empty, HttpStatusCode.OK));
    }

    public static async Task<IResult> GetMinecraftVersions(IGmlManager gmlManager, string gameLoader, string? minecraftVersion)
    {
        try
        {
            if (!Enum.TryParse<GameLoader>(gameLoader, out var loader))
            {
                return Results.BadRequest(ResponseMessage.Create("Не удалось определить вид загрузчика",
                    HttpStatusCode.BadRequest));
            }

            var versions = await gmlManager.Profiles.GetAllowVersions(loader, minecraftVersion);

            return Results.Ok(ResponseMessage.Create(versions, "Доступные версии Minecraft", HttpStatusCode.OK));
        }
        catch (VersionNotLoadedException versionNotLoadedException)
        {
            return Results.NotFound(ResponseMessage.Create(versionNotLoadedException.InnerExceptionMessage, HttpStatusCode.NotFound));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
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
                LoaderVersion = context.Request.Form["LoaderVersion"],
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

            if (!await gmlManager.Profiles.CanAddProfile(createDto.Name, createDto.Version, createDto.LoaderVersion, createDto.GameLoader))
                return Results.BadRequest(ResponseMessage.Create("Невозможно создать профиль по полученным данным",
                    HttpStatusCode.BadRequest));

            if (context.Request.Form.Files.FirstOrDefault() is { } formFile)
                createDto.IconBase64 = await systemService.GetBase64FromImageFile(formFile);

            var profile = await gmlManager.Profiles.AddProfile(createDto.Name, createDto.Version, createDto.LoaderVersion, createDto.GameLoader,
                createDto.IconBase64, createDto.Description);

            return Results.Created($"/api/v1/profiles/{createDto.Name}",
                ResponseMessage.Create(mapper.Map<ProfileReadDto>(profile), "Профиль успешно создан",
                    HttpStatusCode.Created));
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            Debug.WriteLine(exception);

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
            OriginalName = context.Request.Form["OriginalName"],
            JvmArguments = context.Request.Form["JvmArguments"],
            GameArguments = context.Request.Form["GameArguments"],
            IsEnabled = context.Request.Form["Enabled"] == "true"
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

        var background = context.Request.Form.Files["background"] is null
            ? null
            : context.Request.Form.Files["background"]!.OpenReadStream();

        await gmlManager.Profiles.UpdateProfile(
            profile,
            updateDto.Name,
            icon,
            background,
            updateDto.Description,
            updateDto.IsEnabled,
            updateDto.JvmArguments,
            updateDto.GameArguments);

        var newProfile = mapper.Map<ProfileReadDto>(profile);
        newProfile.Background = $"{context.Request.Scheme}://{context.Request.Host}/api/v1/file/{profile.BackgroundImageKey}";

        var message = $"""Профиль "{updateDto.Name}" успешно обновлен""";

        await gmlManager.Notifications.SendMessage("Обновление профиля", message, NotificationType.Info);

        return Results.Ok(ResponseMessage.Create(newProfile, message, HttpStatusCode.OK));
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

        var profile = await gmlManager.Profiles.GetProfile(restoreDto.Name);

        if (profile is null)
            return Results.NotFound(ResponseMessage.Create("Профиль не найден", HttpStatusCode.NotFound));

        await gmlManager.Profiles.RestoreProfileInfo(profile.Name);

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
            return Results.BadRequest(ResponseMessage.Create("Не удалось определить вид операционной системы профиля",
                HttpStatusCode.BadRequest));

        var osName = SystemHelper.GetStringOsType(osType);

        var profile = await gmlManager.Profiles.GetProfile(createInfoDto.ProfileName);

        if (profile is null)
            return Results.NotFound(ResponseMessage.Create($"Профиль \"{createInfoDto.ProfileName}\" не найден",
                HttpStatusCode.NotFound));

        var token = context.Request.Headers["Authorization"].FirstOrDefault();

        var user = await gmlManager.Users.GetUserByName(createInfoDto.UserName);

        if (user is null || user.AccessToken != token || user.IsBanned)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        if (profile.UserWhiteListGuid.Count != 0 && !profile.UserWhiteListGuid.Any(c => c.Equals(user.Uuid, StringComparison.OrdinalIgnoreCase)))
            return Results.Forbid();

        user.Manager = gmlManager;

        var profileInfo = await gmlManager.Profiles.GetProfileInfo(profile.Name, new StartupOptions
        {
            FullScreen = createInfoDto.IsFullScreen,
            ServerIp = createInfoDto.GameAddress,
            ServerPort = createInfoDto.GamePort,
            ScreenHeight = createInfoDto.WindowHeight,
            ScreenWidth = createInfoDto.WindowWidth,
            MaximumRamMb = createInfoDto.RamSize,
            MinimumRamMb = createInfoDto.RamSize,
            OsName = osName,
            OsArch = createInfoDto.OsArchitecture
        },user);

        var profileDto = mapper.Map<ProfileReadInfoDto>(profileInfo);

        profileDto.Background = $"{context.Request.Scheme}://{context.Request.Host}/api/v1/file/{profile.BackgroundImageKey}";

        return Results.Ok(ResponseMessage.Create(profileDto, string.Empty, HttpStatusCode.OK));
    }

    public static async Task<IResult> GetProfileDetails(
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
            return Results.BadRequest(ResponseMessage.Create("Не удалось определить вид операционной системы профиля",
                HttpStatusCode.BadRequest));

        var osName = SystemHelper.GetStringOsType(osType);

        var profile = await gmlManager.Profiles.GetProfile(createInfoDto.ProfileName);

        if (profile is null)
            return Results.NotFound(ResponseMessage.Create($"Профиль \"{createInfoDto.ProfileName}\" не найден",
                HttpStatusCode.NotFound));

        var user = new AuthUser
        {
            AccessToken = new string('0', 50),
            Uuid = Guid.NewGuid().ToString(),
            Name = "GmlAdmin",
            Manager = gmlManager
        };

        var profileInfo = await gmlManager.Profiles.GetProfileInfo(profile.Name, new StartupOptions
        {
            FullScreen = createInfoDto.IsFullScreen,
            ServerIp = createInfoDto.GameAddress,
            ServerPort = createInfoDto.GamePort,
            ScreenHeight = createInfoDto.WindowHeight,
            ScreenWidth = createInfoDto.WindowWidth,
            MaximumRamMb = createInfoDto.RamSize,
            MinimumRamMb = createInfoDto.RamSize,
            OsName = osName,
            OsArch = createInfoDto.OsArchitecture
        }, user);

        var whiteListPlayers = await gmlManager.Users.GetUsers(profile.UserWhiteListGuid);

        var profileDto = mapper.Map<ProfileReadInfoDto>(profileInfo);

        profileDto.Background = $"{context.Request.Scheme}://{context.Request.Host}/api/v1/file/{profile.BackgroundImageKey}";
        profileDto.IsEnabled = profile.IsEnabled;
        profileDto.UsersWhiteList = mapper.Map<List<PlayerReadDto>>(whiteListPlayers);

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

            if (profile == null || profile.State == ProfileState.Loading)
                notRemovedProfiles.Add(profileName);
            else
                await gmlManager.Profiles.RemoveProfile(profile, removeFiles);
        }

        var message = "Операция выполнена";

        if (notRemovedProfiles.Any())
        {
            message += ". Было пропущено удаление:";
            message += string.Join(",", notRemovedProfiles);
        }else
        {
            message += $""". Профили: "{profileNames}" удалены.""";
        }

        await gmlManager.Notifications.SendMessage("Удаление профилей", message, NotificationType.Info);

        return Results.Ok(ResponseMessage.Create(message, HttpStatusCode.OK));
    }

    [Authorize]
    public static async Task<IResult> AddPlayerToWhiteList(
        IGmlManager gmlManager,
        IMapper mapper,
        string profileName,
        string userUuid)
    {
        var profile = await gmlManager.Profiles.GetProfile(profileName);

        if (profile is null)
            return Results.NotFound(ResponseMessage.Create($"Профиль \"{profileName}\" не найден",
                HttpStatusCode.NotFound));

        var user = await gmlManager.Users.GetUserByUuid(userUuid);

        if (user is null)
            return Results.NotFound(ResponseMessage.Create($"Пользователь с UUID: \"{userUuid}\" не найден",
                HttpStatusCode.NotFound));

        if (profile.UserWhiteListGuid.Any(c => c.Equals(userUuid)))
            return Results.BadRequest(ResponseMessage.Create($"Пользователь с UUID: \"{userUuid}\" уже находится белом списке пользователей профиля",
                HttpStatusCode.BadRequest));

        profile.UserWhiteListGuid.Add(user.Uuid);
        await gmlManager.Profiles.SaveProfiles();

        var mappedUser = mapper.Map<PlayerReadDto>(user);

        return Results.Ok(ResponseMessage.Create(mappedUser, "Пользователь успешно добавлен в белый список профиля", HttpStatusCode.OK));
    }

    [Authorize]
    public static async Task<IResult> GetMods(
        IGmlManager gmlManager,
        IMapper mapper,
        string profileName)
    {
        var profile = await gmlManager.Profiles.GetProfile(profileName);

        if (profile is null)
            return Results.NotFound(ResponseMessage.Create($"Профиль \"{profileName}\" не найден",
                HttpStatusCode.NotFound));

        var mods = await profile.GetModsAsync();

        return Results.Ok(ResponseMessage.Create(mapper.Map<List<ModReadDto>>(mods), "Список модов успешно получен", HttpStatusCode.OK));
    }

    public static async Task<IResult> GetOptionalsMods(
        IGmlManager gmlManager,
        IMapper mapper,
        string profileName)
    {
        var profile = await gmlManager.Profiles.GetProfile(profileName);

        if (profile is null)
            return Results.NotFound(ResponseMessage.Create($"Профиль \"{profileName}\" не найден",
                HttpStatusCode.NotFound));

        var mods = await profile.GetOptionalsModsAsync();

        return Results.Ok(ResponseMessage.Create(mapper.Map<List<ModReadDto>>(mods), "Список модов успешно получен", HttpStatusCode.OK));
    }

    public static async Task<IResult> FindMods(
        IGmlManager gmlManager,
        IMapper mapper,
        string profileName,
        string modName,
        short offset,
        short take)
    {
        var profile = await gmlManager.Profiles.GetProfile(profileName);

        if (profile is null)
            return Results.NotFound(ResponseMessage.Create($"Профиль \"{profileName}\" не найден",
                HttpStatusCode.NotFound));

        var mods = await gmlManager.Mods.FindModsAsync(profile.Loader, profile.GameVersion, modName, take, offset);

        return Results.Ok(ResponseMessage.Create(mapper.Map<List<ExtendedModReadDto>>(mods.OfType<ModrinthMod>()), "Список модов успешно получен", HttpStatusCode.OK));
    }

    [Authorize]
    public static async Task<IResult> RemovePlayerFromWhiteList(
        IGmlManager gmlManager,
        string profileName,
        string userUuid)
    {
        var profile = await gmlManager.Profiles.GetProfile(profileName);

        if (profile is null)
            return Results.NotFound(ResponseMessage.Create($"Профиль \"{profileName}\" не найден",
                HttpStatusCode.NotFound));

        var user = await gmlManager.Users.GetUserByUuid(userUuid);

        if (user is null)
            return Results.NotFound(ResponseMessage.Create($"Пользователь с UUID: \"{userUuid}\" не найден",
                HttpStatusCode.NotFound));

        if (!profile.UserWhiteListGuid.Any(c=> c.Equals(userUuid)))
            return Results.BadRequest(ResponseMessage.Create($"Пользователь с UUID: \"{userUuid}\" не найден в белом списке пользователей профиля",
                HttpStatusCode.BadRequest));

        profile.UserWhiteListGuid.Remove(user.Uuid);
        await gmlManager.Profiles.SaveProfiles();

        return Results.Ok(ResponseMessage.Create("Пользователь успешно удален из белого списка профиля", HttpStatusCode.OK));
    }
}
