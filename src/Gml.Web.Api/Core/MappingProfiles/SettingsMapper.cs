using AutoMapper;
using Gml.Web.Api.Domains.Settings;
using Gml.Web.Api.Dto.Settings;

namespace Gml.Web.Api.Core.MappingProfiles;

public class SettingsMapper : Profile
{
    public SettingsMapper()
    {
        CreateMap<SettingsUpdateDto, Settings>();
        CreateMap<Settings, SettingsReadDto>();
    }
}
