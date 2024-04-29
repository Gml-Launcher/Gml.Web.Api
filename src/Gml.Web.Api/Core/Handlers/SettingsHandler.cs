using System.Net;
using AutoMapper;
using Gml.Web.Api.Core.Repositories;
using Gml.Web.Api.Domains.Settings;
using Gml.Web.Api.Dto.Messages;
using Gml.Web.Api.Dto.Settings;
using Newtonsoft.Json;

namespace Gml.Web.Api.Core.Handlers;

public abstract class SettingsHandler : ISettingsHandler
{
    public static async Task<IResult> UpdateSettings(
        ISettingsRepository settingsService,
        IMapper mapper,
        SettingsUpdateDto settingsDto)
    {
        var settings = mapper.Map<Settings>(settingsDto);

        var result = await settingsService.UpdateSettings(settings);

        return Results.Ok(ResponseMessage.Create(mapper.Map<SettingsReadDto>(result), "Настройки обновлены", HttpStatusCode.OK));
    }
    public static async Task<IResult> GetSettings(ISettingsRepository settingsService, IMapper mapper)
    {
        var settings = await settingsService.GetSettings();

        return Results.Ok(ResponseMessage.Create(mapper.Map<SettingsReadDto>(settings), "Настройки обновлены", HttpStatusCode.OK));
    }
}
