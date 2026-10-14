using Mapster;
using Gml.Domains.Integrations;
using Gml.Dto.Integration;
using GmlCore.Interfaces.Integrations;

namespace Gml.Web.Api.Core.MappingProfiles;

public class DiscordRpcMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<DiscordRpcUpdateDto, DiscordRpcClient>();
        config.NewConfig<IDiscordRpcClient, DiscordRpcReadDto>();
    }
}
