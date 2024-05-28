using AutoMapper;
using Gml.Web.Api.Domains.Integrations;
using Gml.Web.Api.Dto.Integration;
using GmlCore.Interfaces.Integrations;

namespace Gml.Web.Api.Core.MappingProfiles;

public class DiscordRpcMapper : Profile
{
    public DiscordRpcMapper()
    {
        CreateMap<DiscordRpcUpdateDto, DiscordRpcClient>();
        CreateMap<IDiscordRpcClient, DiscordRpcReadDto>();
    }
}
