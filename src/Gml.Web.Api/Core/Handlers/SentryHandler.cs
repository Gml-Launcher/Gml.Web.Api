using System.Net;
using System.Text.Json;
using Gml.Core.Launcher;
using Gml.Models.Converters;
using Gml.Web.Api.Core.Repositories;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Dto.Messages;
using Gml.Web.Api.Dto.Sentry;
using GmlCore.Interfaces;
using Newtonsoft.Json;
using MemoryInfo = Gml.Core.Launcher.MemoryInfo;

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
            PcName = sentryModules.ServerName ?? "Not found",
            Username = sentryModules.User.Username ?? "Not found",
            MemoryInfo = new MemoryInfo
            {
                AllocatedBytes = sentryModules.Contexts.MemoryInfo.AllocatedBytes,
                HighMemoryLoadThresholdBytes = sentryModules.Contexts.MemoryInfo.HighMemoryLoadThresholdBytes,
                TotalAvailableMemoryBytes = sentryModules.Contexts.MemoryInfo.TotalAvailableMemoryBytes,
                Compacted = sentryModules.Contexts.MemoryInfo.Compacted,
                Concurrent = sentryModules.Contexts.MemoryInfo.Concurrent,
                PauseDurations = sentryModules.Contexts.MemoryInfo.PauseDurations,
            },
            Exceptions = sentryModules.Exception.Values.Select(x => new ExceptionReport
            {
                Type = x.Type ?? "Not Found",
                ValueData = x.ValueData ?? "Not Found",
                Module = x.Module ?? "Not Found",
                ThreadId = x.ThreadId,
                Id = x.Id,
                Crashed = x.Crashed,
                Current = x.Current,
                StackTrace = sentryModules.Exception.Values.SelectMany(x => x.Stacktrace.Frames.Select(frame => new StackTrace
                {
                    Filename = frame.Filename,
                    Function = frame.Function,
                    Lineno = frame.Lineno,
                    Colno = frame.Colno,
                    AbsPath = frame.AbsPath ?? "Not Found",
                    InApp = frame.InApp,
                    Package = frame.Package,
                    InstructionAddr = frame.InstructionAddr,
                    AddrMode = frame.AddrMode,
                    FunctionId = frame.FunctionId
                 })) ?? [],
            }),
            SendAt = sentryEvent.SentAt,
            IpAddress = sentryModules.User.IpAddress ?? "Not found",
            OsVeriosn = sentryModules.Contexts.Os.RawDescription,
            OsIdentifier = sentryModules.Contexts.Runtime.Type
        });

        return Results.Empty;
    }

    public static async Task<IResult> GetBugs(IGmlManager gmlManager)
    {
        var bugs = await gmlManager.BugTracker.GetAllBugs();

        return Results.Ok(ResponseMessage.Create(bugs, "Bugs Retrieved", HttpStatusCode.OK));
    }

    public static async Task<IResult> GetBugId(IGmlManager gmlManager, string id)
    {
        var bug = await gmlManager.BugTracker.GetBugId(id);

        if (bug is null)
            return Results.BadRequest(ResponseMessage.Create("Ошибка не найдена", HttpStatusCode.BadRequest));

        return Results.Ok(ResponseMessage.Create(bug, "Bug info", HttpStatusCode.OK));
    }
}
