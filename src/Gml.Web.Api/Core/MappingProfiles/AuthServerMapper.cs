using AutoMapper;
using Gml.Dto.Integration;
using Gml.Models.Auth;

namespace Gml.Web.Api.Core.MappingProfiles;

public class AuthServerMapper : Profile
{
    public AuthServerMapper()
    {
        CreateMap<AuthServiceInfo, AuthServiceReadDto>();
    }
}
