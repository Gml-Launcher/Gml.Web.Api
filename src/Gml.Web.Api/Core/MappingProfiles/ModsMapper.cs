using AutoMapper;
using Gml.Models.Mods;
using Gml.Web.Api.Dto.Mods;
using GmlCore.Interfaces.Mods;
using Modrinth.Api.Models.Dto.Entities;

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

