using System.Net;
using AutoMapper;
using FluentValidation;
using Gml.Models.System;
using Gml.Web.Api.Dto.Files;
using Gml.Web.Api.Dto.Messages;
using GmlCore.Interfaces;
using GmlCore.Interfaces.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gml.Web.Api.Core.Handlers;

public class FileHandler : IFileHandler
{
    public static async Task GetFile(HttpContext context, IGmlManager gmlManager, string fileHash)
    {
        var response = context.Response;

        var (file, fileName, length) = await gmlManager.Files.GetFileStream(fileHash);

        try
        {
            response.Headers.Append("Content-Disposition", $"attachment; filename={fileName}");
            response.Headers.Append("Content-Length", length.ToString());
            response.ContentType = "application/octet-stream";

            await file.CopyToAsync(response.Body);
        }
        catch (Exception exception)
        {
            Console.WriteLine(fileName + exception);
            gmlManager.BugTracker.CaptureException(exception);
        }
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

        fileDto = fileDto.DistinctBy(c => c.Directory).ToList();

        var profileNames = fileDto.GroupBy(c => c.ProfileName);

        foreach (var profileFiles in profileNames)
        {
            var profile = await manager.Profiles.GetProfile(profileFiles.Key);

            if (profile == null)
                return Results.NotFound(ResponseMessage.Create($"Профиль с именем \"{profileFiles.Key}\" не найден",
                    HttpStatusCode.NotFound));

            foreach (var fileInfo in profileFiles)
            {
                await manager.Profiles.AddFileToWhiteList(profile, new LocalFileInfo(fileInfo.Directory));
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

        fileDto = fileDto.DistinctBy(c => c.Directory).ToList();

        var profileNames = fileDto.GroupBy(c => c.ProfileName);

        foreach (var profileFiles in profileNames)
        {
            var profile = await manager.Profiles.GetProfile(profileFiles.Key);

            if (profile == null)
                return Results.NotFound(ResponseMessage.Create($"Профиль с именем \"{profileFiles.Key}\" не найден",
                    HttpStatusCode.NotFound));

            foreach (var fileInfo in profileFiles.DistinctBy(c => c.Directory))
            {
                await manager.Profiles.RemoveFileFromWhiteList(profile, new LocalFileInfo(fileInfo.Directory));
            }
        }

        return Results.Ok(ResponseMessage.Create($"\"{fileDto.Count}\" файлов было успешно удалено из White-Листа",
            HttpStatusCode.OK));
    }

    public static async Task<IResult> AddFolderWhiteList(
        IGmlManager manager,
        IMapper mapper,
        IValidator<List<FolderWhiteListDto>> validator,
        [FromBody] List<FolderWhiteListDto> folderDto)
    {
        var result = await validator.ValidateAsync(folderDto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        folderDto = folderDto.DistinctBy(x => x.Path).ToList();

        var mappedFolders = mapper.Map<List<LocalFolderInfo>>(folderDto);

        var profileNames = folderDto.GroupBy(c => c.ProfileName);

        foreach (var profileFolders in profileNames)
        {
            var profile = await manager.Profiles.GetProfile(profileFolders.Key);

            if (profile == null)
                return Results.NotFound(ResponseMessage.Create($"Профиль с именем \"{profileFolders.Key}\" не найден",
                    HttpStatusCode.NotFound));

            await manager.Profiles.AddFolderToWhiteList(profile, mappedFolders);
        }

        return Results.Ok(ResponseMessage.Create($"\"{folderDto.Count}\" папок было успешно добавлено в White-Лист",
            HttpStatusCode.OK));
    }

    public static async Task<IResult> RemoveFolderWhiteList(
        IGmlManager manager,
        IMapper mapper,
        IValidator<List<FolderWhiteListDto>> validator,
        [FromBody] List<FolderWhiteListDto> folderDto)
    {
        var result = await validator.ValidateAsync(folderDto);

        if (!result.IsValid)
            return Results.BadRequest(ResponseMessage.Create(result.Errors, "Ошибка валидации",
                HttpStatusCode.BadRequest));

        folderDto = folderDto.DistinctBy(x => x.Path).ToList();

        var mappedFolders = mapper.Map<List<LocalFolderInfo>>(folderDto);

        var profileNames = folderDto.GroupBy(c => c.ProfileName);

        foreach (var profileFolders in profileNames)
        {
            var profile = await manager.Profiles.GetProfile(profileFolders.Key);

            if (profile == null)
                return Results.NotFound(ResponseMessage.Create($"Профиль с именем \"{profileFolders.Key}\" не найден",
                    HttpStatusCode.NotFound));

            await manager.Profiles.RemoveFolderFromWhiteList(profile, mappedFolders);
        }

        return Results.Ok(ResponseMessage.Create($"\"{folderDto.Count}\" папок было успешно удалено из White-Лист",
            HttpStatusCode.OK));
    }
}
