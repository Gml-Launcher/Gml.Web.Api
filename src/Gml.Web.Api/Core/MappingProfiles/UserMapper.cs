using AutoMapper;
using Gml.Domains.Integrations;
using Gml.Domains.User;
using Gml.Dto.User;

namespace Gml.Web.Api.Core.MappingProfiles;

public class UserMapper : Profile
{
    public UserMapper()
    {
        CreateMap<DbUser, UserAuthReadDto>();
    }
}
