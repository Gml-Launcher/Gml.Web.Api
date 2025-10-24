using AutoMapper;
using Gml.Dto.Servers;
using Gml.Models.Servers;
using Gml.Web.Api.Domains.Servers;

namespace Gml.Web.Api.Core.MappingProfiles;

public class ServerMapper : Profile
{
    public ServerMapper()
    {
        CreateMap<CreateServerDto, MinecraftServer>();
        CreateMap<MinecraftServer, ServerReadDto>();
    }
}
