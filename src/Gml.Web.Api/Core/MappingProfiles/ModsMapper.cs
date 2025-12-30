using AutoMapper;
using Gml.Dto.Mods;
using Gml.Models.Mods;
using GmlCore.Interfaces.Mods;
using Modrinth.Models;

namespace Gml.Web.Api.Core.MappingProfiles;

public class ModsMapper : Profile
{
    public ModsMapper()
    {
        CreateMap<IMod, ModReadDto>();
        CreateMap<IMod, ExtendedModReadDto>();
        CreateMap<IExternalMod, ExtendedModReadDto>();
        CreateMap<IExternalMod, ExtendedModInfoReadDto>();
        CreateMap<ModrinthModVersion, ModVersionDto>();
        CreateMap<CurseForgeModVersion, ModVersionDto>();
        CreateMap<Dependency, ModVersionDtoDependency>();
    }
}
