using Gml.Core.Launcher;
using Gml.Web.Api.Core.Services;
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
            PcName = sentryModules.ServerName,
            Username = sentryModules.User.Username,
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
                Type = x.Type,
                ValueData = x.ValueData,
                Module = x.Module,
                ThreadId = x.ThreadId,
                Id = x.Id,
                Crashed = x.Crashed,
                Current = x.Current,
                StackTrace = x.Stacktrace.Frames.Select(x => new StackTrace
                {
                    Filename = x.Filename,
                    Function = x.Function,
                    Lineno = x.Lineno,
                    Colno = x.Colno,
                    AbsPath = x.AbsPath,
                    InApp = x.InApp,
                    Package = x.Package,
                    InstructionAddr = x.InstructionAddr,
                    AddrMode = x.AddrMode,
                    FunctionId = x.FunctionId
                }),

            }),
            SendAt = sentryEvent.SentAt,
            IpAddress = sentryModules.User.IpAddress,
            OsVeriosn = sentryModules.Contexts.Os.RawDescription,
            OsIdentifier = sentryModules.Contexts.Runtime.Type
        });

        return Results.Empty;
    }
}
