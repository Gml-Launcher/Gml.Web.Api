using Gml.Core.Launcher;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Dto.Sentry;
using GmlCore.Interfaces;
using Newtonsoft.Json;

namespace Gml.Web.Api.Core.Handlers;

public abstract class SentryHandler : ISentryHandler
{
    public static async Task<IResult> GetMessage(HttpContext context, IGmlManager gmlManager, int projectId)
    {
        byte[] compressedData;

        using (var memoryStream = new MemoryStream())
        {
            await context.Request.Body.CopyToAsync(memoryStream);
            compressedData = memoryStream.ToArray();
        }

        string uncompressedContent = await CompressionService.Uncompress(compressedData);

        string[] jsonObjects = uncompressedContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        var sentryEvent = JsonConvert.DeserializeObject<SentryEventDto>(jsonObjects[0]);
        var sentryLength = JsonConvert.DeserializeObject<SentryEventLengthDto>(jsonObjects[1]);
        var sentryModules = JsonConvert.DeserializeObject<SentryModulesDto>(jsonObjects[2]);
        var fileContent = string.Empty;

        if (jsonObjects.Length >= 4)
            fileContent = string.Join('\n', jsonObjects.Skip(4).Take(jsonObjects.Length - 4).ToArray());

        if (sentryModules is not null && sentryModules.User.IpAddress.Equals("{{auto}}"))
            sentryModules.User.IpAddress = context.Request.Headers["X-Forwarded-For"];

        gmlManager.BugTracker.CaptureException(new BugInfo
        {

        });

        return Results.Empty;
    }
}
