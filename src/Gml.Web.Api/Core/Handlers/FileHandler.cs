using System.Net;
using FluentValidation;
using Gml.Web.Api.Dto.Files;
using Gml.Web.Api.Dto.Messages;
using GmlCore.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gml.Web.Api.Core.Handlers;

public class FileHandler : IFileHandler
{
    public static async Task<IResult> GetFile(IGmlManager manager, string fileHash)
    {
        var file = await manager.Files.GetFileInfo(fileHash);

        if (file == null)
            return Results.NotFound(ResponseMessage.Create("Информация по файлу не найдена", HttpStatusCode.NotFound));

        return Results.File(string.Join("/", manager.LauncherInfo.InstallationDirectory, file.Directory));
    }

    [Authorize]
    public static async Task<IResult> AddFileWhiteList(IGmlManager manager, IValidator<FileWhiteListDto> validator,
        [FromBody] FileWhiteListDto fileDto)
    {
        var result = await validator.ValidateAsync(fileDto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        var profile = await manager.Profiles.GetProfile(fileDto.ProfileName);

        if (profile == null)
            return Results.NotFound(ResponseMessage.Create($"Профиль с именем \"{fileDto.ProfileName}\" не найден",
                HttpStatusCode.NotFound));

        var file = await manager.Files.GetFileInfo(fileDto.Hash);

        if (file == null)
            return Results.NotFound(ResponseMessage.Create("Информация по файлу не найдена", HttpStatusCode.NotFound));

        await manager.Profiles.AddFileToWhiteList(profile, file);

        return Results.Ok(ResponseMessage.Create($"Файл \"{file.Name}\" успешно добавлен в White-Лист",
            HttpStatusCode.NotFound));
    }

    [Authorize]
    public static async Task<IResult> RemoveFileWhiteList(IGmlManager manager, IValidator<FileWhiteListDto> validator,
        [FromBody] FileWhiteListDto fileDto)
    {
        var result = await validator.ValidateAsync(fileDto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        var profile = await manager.Profiles.GetProfile(fileDto.ProfileName);

        if (profile == null)
            return Results.NotFound(ResponseMessage.Create($"Профиль с именем \"{fileDto.ProfileName}\" не найден",
                HttpStatusCode.NotFound));

        var file = await manager.Files.GetFileInfo(fileDto.Hash);

        if (file == null)
            return Results.NotFound(ResponseMessage.Create("Информация по файлу не найдена", HttpStatusCode.NotFound));

        await manager.Profiles.RemoveFileFromWhiteList(profile, file);

        return Results.Ok(ResponseMessage.Create($"Файл \"{file.Name}\" успешно удален из White-Листа",
            HttpStatusCode.NotFound));
    }
}
