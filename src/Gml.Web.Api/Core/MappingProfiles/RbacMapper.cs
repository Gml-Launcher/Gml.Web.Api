using Mapster;
using Gml.Domains.Auth;
using Gml.Dto.Auth;

namespace Gml.Web.Api.Core.MappingProfiles;

public class RbacMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Role, RoleDto>();
        config.NewConfig<Permission, PermissionDto>();
    }
}
