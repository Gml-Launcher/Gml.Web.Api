using Mapster;
using Gml.Core.Launcher;
using Gml.Dto.Profile;
using Gml.Models;

namespace Gml.Web.Api.Core.MappingProfiles;

public class ProfileMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<GameProfile, ProfileReadDto>();
        config.NewConfig<GameProfileInfo, ProfileReadInfoDto>();
    }
}
