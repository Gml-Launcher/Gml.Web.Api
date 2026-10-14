using Mapster;
using Gml.Domains.Settings;
using Gml.Dto.Settings;

namespace Gml.Web.Api.Core.MappingProfiles;

public class SettingsMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<SettingsUpdateDto, Settings>();
        config.NewConfig<Settings, SettingsReadDto>();
    }
}
