using AutoMapper;
using Gml.Core.User;
using Gml.Web.Api.Dto.Player;

namespace Gml.Web.Api.Core.MappingProfiles;

public class PlayerMapper : Profile
{
    public PlayerMapper()
    {
        CreateMap<AuthUser, PlayerReadDto>();
        CreateMap<AuthUser, ExtendedPlayerReadDto>();
        CreateMap<AuthUser, PlayerTextureDto>();
        CreateMap<AuthUserHistory, AuthUserHistoryDto>();
        CreateMap<ServerJoinHistory, ServerJoinHistoryDto>();
    }
}
