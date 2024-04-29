using AutoMapper;
using Gml.Web.Api.Domains.User;
using Gml.Web.Api.Dto.User;

namespace Gml.Web.Api.Core.MappingProfiles;

public class UserMapper : Profile
{
    public UserMapper()
    {
        CreateMap<User, UserAuthReadDto>();
    }
}
