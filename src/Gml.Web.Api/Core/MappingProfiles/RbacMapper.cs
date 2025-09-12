using AutoMapper;
using Gml.Web.Api.Domains.Auth;
using Gml.Web.Api.Dto.Auth;

namespace Gml.Web.Api.Core.MappingProfiles;

public class RbacMapper : Profile
{
    public RbacMapper()
    {
        CreateMap<Role, RoleDto>();
        CreateMap<Permission, PermissionDto>();
    }
}
