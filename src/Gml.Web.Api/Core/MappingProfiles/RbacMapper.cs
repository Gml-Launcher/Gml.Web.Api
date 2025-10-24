using AutoMapper;
using Gml.Domains.Auth;
using Gml.Dto.Auth;

namespace Gml.Web.Api.Core.MappingProfiles;

public class RbacMapper : Profile
{
    public RbacMapper()
    {
        CreateMap<Role, RoleDto>();
        CreateMap<Permission, PermissionDto>();
    }
}
