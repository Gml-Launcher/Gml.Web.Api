using AutoMapper;
using Gml.Models.Mods;
using Gml.Web.Api.Dto.Mods;
using GmlCore.Interfaces.Mods;

namespace Gml.Web.Api.Core.MappingProfiles;

public class ModsMapper : Profile
{
    public ModsMapper()
    {
        CreateMap<IMod, ModReadDto>();
        CreateMap<IMod, ExtendedModReadDto>();
        CreateMap<IExternalMod, ExtendedModInfoReadDto>();
        CreateMap<ModrinthMod, ExtendedModReadDto>();
    }
}
