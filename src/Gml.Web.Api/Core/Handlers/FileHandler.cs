using System.Net;
using System.Net.Http.Headers;
using FluentValidation;
using Gml.Web.Api.Dto.Files;
using Gml.Web.Api.Dto.Messages;
using GmlCore.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gml.Web.Api.Core.Handlers;

public class FileHandler : IFileHandler
{
    public static async Task GetFile(HttpContext context ,IGmlManager gmlManager, string fileHash)
    {
        var response = context.Response;

        response.ContentType = "application/octet-stream";

        context.Response.ContentType = "application/octet-stream";

        await gmlManager.Files.DownloadFileStream(fileHash, response.Body, response.Headers);
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

        var file = await manager.Files.DownloadFileStream(fileDto.Hash, new MemoryStream(), null);

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

        var file = await manager.Files.DownloadFileStream(fileDto.Hash, new MemoryStream(), null);

        if (file == null)
            return Results.NotFound(ResponseMessage.Create("Информация по файлу не найдена", HttpStatusCode.NotFound));

        await manager.Profiles.RemoveFileFromWhiteList(profile, file);

        return Results.Ok(ResponseMessage.Create($"Файл \"{file.Name}\" успешно удален из White-Листа",
            HttpStatusCode.NotFound));
    }
}
