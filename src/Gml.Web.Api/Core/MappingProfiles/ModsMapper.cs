using Mapster;
using Gml.Dto.Mods;
using Gml.Models.Mods;
using GmlCore.Interfaces.Mods;
using Modrinth.Models;

namespace Gml.Web.Api.Core.MappingProfiles;

public class ModsMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<IMod, ModReadDto>();
        config.NewConfig<IMod, ExtendedModReadDto>();
        config.NewConfig<IExternalMod, ExtendedModReadDto>();
        config.NewConfig<IExternalMod, ExtendedModInfoReadDto>();
        config.NewConfig<ModrinthModVersion, ModVersionDto>();
        config.NewConfig<CurseForgeModVersion, ModVersionDto>();
        config.NewConfig<Dependency, ModVersionDtoDependency>();
    }
}
