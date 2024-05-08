using AutoMapper;
using Gml.Web.Api.Domains.Integrations;
using Gml.Web.Api.Dto.Integration;

namespace Gml.Web.Api.Core.MappingProfiles;

public class DiscordRPcMapper : Profile
{
    public DiscordRPcMapper()
    {
        CreateMap<DiscordRpcUpdateDto, DiscordRpcClient>();
        CreateMap<DiscordRpcClient, DiscordRpcReadDto>();
    }
}
