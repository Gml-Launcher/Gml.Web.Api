using AutoMapper;
using Gml.Models.Auth;
using Gml.Web.Api.Dto.Integration;

namespace Gml.Web.Api.Core.MappingProfiles;

public class AuthServerMapper : Profile
{
    public AuthServerMapper()
    {
        CreateMap<AuthServiceInfo, AuthServiceReadDto>();
    }
}
