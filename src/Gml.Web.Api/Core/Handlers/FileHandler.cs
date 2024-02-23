using System.Net;
using Gml.Web.Api.Core.Messages;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Handlers;

public class FileHandler : IFileHandler
{
    public static async Task<IResult> GetFile(IGmlManager manager, string fileHash)
    {
        var file = await manager.Files.GetFileInfo(fileHash);

        if (file == null)
        {
            return Results.NotFound(ResponseMessage.Create("Информация по файлу не найдена", HttpStatusCode.NotFound));
        }
        
        return Results.File(string.Join("/", manager.LauncherInfo.InstallationDirectory, file.Directory));
    }
}