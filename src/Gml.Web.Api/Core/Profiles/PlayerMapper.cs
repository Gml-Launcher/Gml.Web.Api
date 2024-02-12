using AutoMapper;
using Gml.Core.User;
using Gml.Web.Api.Dto.Player;
using Gml.Web.Api.Dto.Profile;

namespace Gml.Web.Api.Core.Profiles;

public class PlayerMapper : Profile
{

    public PlayerMapper()
    {
        CreateMap<AuthUser, PlayerReadDto>();
    }
    
}