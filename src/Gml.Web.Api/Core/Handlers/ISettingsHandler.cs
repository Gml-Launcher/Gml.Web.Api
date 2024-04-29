using AutoMapper;
using Gml.Web.Api.Core.Repositories;
using Gml.Web.Api.Dto.Settings;

namespace Gml.Web.Api.Core.Handlers;

public interface ISettingsHandler
{
    static abstract Task<IResult> UpdateSettings(
        ISettingsRepository settingsService,
        IMapper mapper,
        SettingsUpdateDto settingsDto);

    static abstract Task<IResult> GetSettings(
        ISettingsRepository settingsService,
        IMapper mapper);
}
