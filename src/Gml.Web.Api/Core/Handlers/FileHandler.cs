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
    public static async Task GetFile(HttpContext context, IGmlManager gmlManager, string fileHash)
    {
        var response = context.Response;

        (Stream file, string fileName, long length) = await gmlManager.Files.GetFileStream(fileHash);

        response.Headers.Append("Content-Disposition", $"attachment; filename={fileName}");
        response.Headers.Append("Content-Length", length.ToString());
        response.ContentType = "application/octet-stream";

        await file.CopyToAsync(response.Body);

    }

    [Authorize]
    public static async Task<IResult> AddFileWhiteList(
        IGmlManager manager,
        IValidator<List<FileWhiteListDto>> validator,
        [FromBody] List<FileWhiteListDto> fileDto)
    {
        var result = await validator.ValidateAsync(fileDto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        fileDto = fileDto.DistinctBy(c => c.Hash).ToList();

        var profileNames = fileDto.GroupBy(c => c.ProfileName);

        foreach (var profileFiles in profileNames)
        {
            var profile = await manager.Profiles.GetProfile(profileFiles.Key);

            if (profile == null)
                return Results.NotFound(ResponseMessage.Create($"Профиль с именем \"{profileFiles.Key}\" не найден",
                    HttpStatusCode.NotFound));

            foreach (var fileInfo in profileFiles)
            {
                var file = await manager.Files.DownloadFileStream(fileInfo.Hash, new MemoryStream(), null);

                if (file == null)
                    return Results.NotFound(ResponseMessage.Create("Информация по файлу не найдена", HttpStatusCode.NotFound));

                await manager.Profiles.AddFileToWhiteList(profile, file);
            }

        }

        return Results.Ok(ResponseMessage.Create($"\"{fileDto.Count}\" файлов было успешно добавлено в White-Лист",
            HttpStatusCode.OK));
    }

    [Authorize]
    public static async Task<IResult> RemoveFileWhiteList(
        IGmlManager manager,
        IValidator<List<FileWhiteListDto>> validator,
        [FromBody] List<FileWhiteListDto> fileDto)
    {
        var result = await validator.ValidateAsync(fileDto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        fileDto = fileDto.DistinctBy(c => c.Hash).ToList();

        var profileNames = fileDto.GroupBy(c => c.ProfileName);

        foreach (var profileFiles in profileNames)
        {
            var profile = await manager.Profiles.GetProfile(profileFiles.Key);

            if (profile == null)
                return Results.NotFound(ResponseMessage.Create($"Профиль с именем \"{profileFiles.Key}\" не найден",
                    HttpStatusCode.NotFound));

            foreach (var fileInfo in profileFiles.DistinctBy(c => c.Hash))
            {
                var file = await manager.Files.DownloadFileStream(fileInfo.Hash, new MemoryStream(), null);

                if (file == null)
                    return Results.NotFound(ResponseMessage.Create("Информация по файлу не найдена",
                        HttpStatusCode.NotFound));

                await manager.Profiles.RemoveFileFromWhiteList(profile, file);
            }
        }

        return Results.Ok(ResponseMessage.Create($"\"{fileDto.Count}\" файлов было успешно удалено из White-Листа",
            HttpStatusCode.OK));
    }
}
