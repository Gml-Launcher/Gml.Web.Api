using AutoMapper;
using Gml.Domains.Settings;
using Gml.Dto.Settings;

namespace Gml.Web.Api.Core.MappingProfiles;

public class SettingsMapper : Profile
{
    public SettingsMapper()
    {
        CreateMap<SettingsUpdateDto, Settings>();
        CreateMap<Settings, SettingsReadDto>();
    }
}
