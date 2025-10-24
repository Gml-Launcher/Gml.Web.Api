using AutoMapper;
using Gml.Dto.Integration;
using Gml.Models.Discord;
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
