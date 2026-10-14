using Mapster;
using Gml.Dto.Player;
using Gml.Models.User;

namespace Gml.Web.Api.Core.MappingProfiles;

public class PlayerMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AuthUser, PlayerReadDto>();
        config.NewConfig<AuthUser, ExtendedPlayerReadDto>();
        config.NewConfig<AuthUser, PlayerTextureDto>();
        config.NewConfig<AuthUserHistory, AuthUserHistoryDto>();
        config.NewConfig<ServerJoinHistory, ServerJoinHistoryDto>();
    }
}