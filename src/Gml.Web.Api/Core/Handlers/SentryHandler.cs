using System.Collections.Frozen;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoMapper;
using Gml.Core.Launcher;
using Gml.Models.Converters;
using Gml.Web.Api.Core.Repositories;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.Domains.Sentry;
using Gml.Web.Api.Domains.System;
using Gml.Web.Api.Dto.Messages;
using Gml.Web.Api.Dto.Sentry;
using Gml.Web.Api.Dto.Sentry.Stats;
using GmlCore.Interfaces;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Sentry;
using Newtonsoft.Json;
using MemoryInfo = Gml.Core.Launcher.MemoryInfo;

namespace Gml.Web.Api.Core.Handlers;

public abstract class SentryHandler : ISentryHandler
{
    public static async Task<IResult> CreateBugInfo(HttpContext context, IGmlManager gmlManager, int projectId)
    {
        byte[] compressedData;

        using (var memoryStream = new MemoryStream())
        {
            await context.Request.Body.CopyToAsync(memoryStream);
            compressedData = memoryStream.ToArray();
        }

        string uncompressedContent = await CompressionService.Uncompress(compressedData);

        string[] jsonObjects = uncompressedContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

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
            ProjectType = ProjectType.Launcher,
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
                StackTrace = sentryModules.Exception.Values.SelectMany(x => x.Stacktrace?.Frames.Select(frame =>
                    new StackTrace
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
                    }) ?? []) ?? [],
            }),
            SendAt = sentryEvent.SentAt,
            IpAddress = sentryModules.User.IpAddress ?? "Not found",
            OsVersion = sentryModules.Contexts.Os.RawDescription,
            OsIdentifier = sentryModules.Contexts.Runtime.Type
        });

        return Results.Empty;
    }

    public static async Task<IResult> SolveAllBugs(IGmlManager gmlManager)
    {
        await gmlManager.BugTracker.SolveAllAsync();

        return Results.Ok(ResponseMessage.Create("Все ошибки очищены", HttpStatusCode.OK));
    }


    public static async Task<IResult> GetBugs(IGmlManager gmlManager, SentryFilterDto filter)
    {
        var minDate = filter.DateFrom ?? DateTime.MinValue;
        var maxDate = filter.DateTo?.Date.AddDays(1).AddTicks(-1) ?? DateTime.MaxValue;
        var bugs = (await gmlManager.BugTracker.GetFilteredBugs(c => c.Date >= minDate && c.Date <= maxDate)).ToFrozenSet();

        var error = new BaseSentryError
        {
            Bugs = bugs
                .Where(c => c.Exceptions.Any())
                .GroupBy(bug => bug.Exceptions.First().Type)
                .Select(group => new SentryBugs
                {
                    Exception = group.Key,
                    Count = group.Count(),
                    CountUsers = group.Select(bug => bug.PcName).Distinct().Count(),
                    Graphics = group
                        .GroupBy(bug => new DateTime(bug.SendAt.Year, bug.SendAt.Month, bug.SendAt.Day))
                        .Select(monthGroup => new SentryGraphic
                        {
                            Month = monthGroup.Key,
                            Count = monthGroup.Count()
                        })
                        .ToList()
                })
                .ToList(),
            CountUsers = bugs.Select(x => x.PcName).Distinct().Count(),
            Count = bugs.Count
        };

        return Results.Ok(ResponseMessage.Create(error, "Все ошибки", HttpStatusCode.OK));
    }

    public static async Task<IResult> GetFilterSentry(IGmlManager gmlManager, SentryFilterDto filter)
    {
        var minDate = filter.DateFrom ?? DateTime.MinValue;
        var maxDate = filter.DateTo?.Date.AddDays(1).AddTicks(-1) ?? DateTime.MaxValue;

        var bugs = await gmlManager.BugTracker.GetFilteredBugs(c => c.Date >= minDate && c.Date <= maxDate);

        var byProject = bugs.Where(c => (filter.ProjectType & c.ProjectType) != 0);

        return Results.Ok(ResponseMessage.Create(byProject, "Отфильтрованные ошибки", HttpStatusCode.OK));
    }

    public static async Task<IResult> GetFilterListSentry(IGmlManager gmlManager, SentryFilterDto filter)
    {
        var minDate = filter.DateFrom ?? DateTime.MinValue;
        var maxDate = filter.DateTo ?? filter.DateTo?.Date.AddDays(1).AddTicks(-1) ?? DateTime.MaxValue;

        var bugs = await gmlManager.BugTracker.GetFilteredBugs(c => c.Date >= minDate && c.Date <= maxDate);

        var byProject = bugs.Where(c => (filter.ProjectType & c.ProjectType) != 0);

        var exceptions = byProject.GroupBy(bug =>
            new {
                BugType = bug.Exceptions.First().Type,
                BugInfo = bug.Exceptions.First().ValueData
            })
            .Select(group => new SentryExceptionReadDto
            {
                Exception = group.Key.BugType,
                Count = group.Count(),
                StackTrace = BuildStackTraceString(group.SelectMany(c => c.Exceptions).DistinctBy(c => c.ValueData).ToFrozenSet()),
                CountUsers = group.Select(bug => bug.PcName).Distinct().Count(),
                OperationSystems = group
                    .Select(x => x.OsVersion)
                    .GroupBy(os => os)
                    .Select(bug => new SentryOperationSystem
                    {
                        Count = bug.Count(), // Количество элементов в группе
                        OsType = bug.Key
                    }),
                Graphic = group
                    .GroupBy(bug => new DateTime(bug.SendAt.Year, bug.SendAt.Month, bug.SendAt.Day))
                    .Select(monthGroup => new SentryGraphic
                    {
                        Month = monthGroup.Key,
                        Count = monthGroup.Count()
                    })
                    .ToList(),
                BugInfo = group.FirstOrDefault()
            });

        return Results.Ok(ResponseMessage.Create(exceptions, "Отфильтрованные ошибки", HttpStatusCode.OK));
    }

    private static string BuildStackTraceString(FrozenSet<IExceptionReport> stackTraceList)
    {
        var builder = new StringBuilder();
        builder.Append(string.Concat(stackTraceList.SelectMany(s => s.ValueData)));
        builder.Append(Environment.NewLine);
        builder.Append(string.Join(Environment.NewLine,
            stackTraceList
                .SelectMany(c => c.StackTrace)
                .Select(st =>
                    $"{st.Function} at {st.Filename}:{st.Lineno} in {st.AbsPath}"
                )));

        return builder.ToString();
    }

    public static async Task<IResult> GetLastSentryErrors(IGmlManager gmlManager, IMapper mapper)
    {
        var maxDate = DateTime.Now;
        var minDate = maxDate.AddMonths(-3);

        var bugs = await gmlManager.BugTracker.GetFilteredBugs(c => c.Date >= minDate && c.Date <= maxDate);

        var mappedBugs = bugs
            .GroupBy(b => b.SendAt.Date)
            .Select(g => new ProjectLastStatsReadDto
            {
                Date = g.Key.Date,
                Launcher = g.Count(b => b.ProjectType == ProjectType.Launcher),
                Backend = g.Count(b => b.ProjectType == ProjectType.Backend)
            })
            .ToList();

        var dateRange = Enumerable.Range(0, (maxDate - minDate).Days + 1)
            .Select(offset => minDate.AddDays(offset).Date)
            .ToList();

        var completeStats = dateRange.GroupJoin(
                mappedBugs,
                date => date,
                stats => stats.Date,
                (date, statsGroup) => new ProjectLastStatsReadDto
                {
                    Date = date,
                    Launcher = statsGroup.Any() ? statsGroup.First().Launcher : 0,
                    Backend = statsGroup.Any() ? statsGroup.First().Backend : 0
                })
            .ToList();

        return Results.Ok(ResponseMessage.Create(completeStats, "Отфильтрованные ошибки", HttpStatusCode.OK));
    }

    public static async Task<IResult> GetSummarySentryErrors(IGmlManager gmlManager, IMapper mapper)
    {
        var today = DateTime.Today.AddDays(1).AddTicks(-1);
        var thisMonthStart = new DateTime(today.Year, today.Month, 1);
        var lastMonthStart = thisMonthStart.AddMonths(-1);
        var lastMonthEnd = thisMonthStart.AddDays(-1);
        var yesterday = today.AddDays(-1);

        // Получение всех багов за последние два месяца
        var allBugs = (await gmlManager.BugTracker.GetFilteredBugs(b => b.Date >= lastMonthStart && b.Date <= today)).ToFrozenSet();

        // Общее количество ошибок
        int totalBugs = allBugs.Count();

        // Ошибки за этот месяц
        int bugsThisMonth = allBugs.Count(b => b.SendAt >= thisMonthStart);

        // Ошибки за прошлый месяц
        int bugsLastMonth = allBugs.Count(b => b.SendAt >= lastMonthStart && b.SendAt <= lastMonthEnd);

        // Расчет процента изменения за месяц
        double percentageChangeMonth = CalculatePercentageChange(bugsLastMonth, bugsThisMonth);

        // Ошибки за сегодня
        int bugsToday = allBugs.Count(b => b.SendAt.Date == today.Date);

        // Ошибки за вчера
        int bugsYesterday = allBugs.Count(b => b.SendAt.Date == yesterday.Date);

        // Расчет процента изменения за день
        double percentageChangeDay = CalculatePercentageChange(bugsYesterday, bugsToday);

        var result = new BugStatisticsReadDto
        {
            TotalBugs = totalBugs,
            BugsThisMonth = bugsThisMonth,
            PercentageChangeMonth = percentageChangeMonth,
            BugsToday = bugsToday,
            PercentageChangeDay = percentageChangeDay,
            FixBugs = 0,
            PercentageChangeDayFixBugs = 0,
        };

        return Results.Ok(ResponseMessage.Create(result, "Статистика", HttpStatusCode.OK));
    }

    private static double CalculatePercentageChange(int previousValue, int currentValue)
    {
        if (previousValue == 0)
        {
            return currentValue == 0 ? 0 : 100;
        }

        return ((double)(currentValue - previousValue) / previousValue) * 100;
    }

    public static async Task<IResult> GetByException(IGmlManager gmlManager, string exception)
    {
        var bugs = (await gmlManager.BugTracker.GetAllBugs()).ToFrozenSet();

        var exceptions = bugs.GroupBy(bug => bug.Exceptions.First().Type == exception)
            .Select(group => new SentryExceptionReadDto
            {
                Count = group.Count(),
                CountUsers = group.Select(bug => bug.PcName).Distinct().Count(),
                OperationSystems = group
                    .Select(x => x.OsVersion)
                    .GroupBy(os => os)
                    .Select(bug => new SentryOperationSystem
                    {
                        Count = bug.Count(), // Количество элементов в группе
                        OsType = bug.Key
                    }),
                Graphic = group
                    .GroupBy(bug => new DateTime(bug.SendAt.Year, bug.SendAt.Month, bug.SendAt.Day))
                    .Select(monthGroup => new SentryGraphic
                    {
                        Month = monthGroup.Key,
                        Count = monthGroup.Count()
                    })
                    .ToList(),
                BugInfo = group.FirstOrDefault()
            });

        return Results.Ok(ResponseMessage.Create(exceptions, "Информация об ошибке", HttpStatusCode.OK));
    }

    public static async Task<IResult> GetBugId(IGmlManager gmlManager, string id)
    {
        var bug = await gmlManager.BugTracker.GetBugId(Guid.Parse(id));

        if (bug is null)
            return Results.BadRequest(ResponseMessage.Create("Ошибка не найдена", HttpStatusCode.BadRequest));

        return Results.Ok(ResponseMessage.Create(bug, "Bug info", HttpStatusCode.OK));
    }
}
