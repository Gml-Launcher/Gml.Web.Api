using Mapster;
using Gml.Dto.Servers;
using Gml.Models.Servers;
using Gml.Web.Api.Domains.Servers;

namespace Gml.Web.Api.Core.MappingProfiles;

public class ServerMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateServerDto, MinecraftServer>();
        config.NewConfig<MinecraftServer, ServerReadDto>();
    }
}
