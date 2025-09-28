using System.Collections.Generic;

namespace Gml.Web.Api.Dto.Auth;

public class UserRolesPermissionsDto
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<RoleDto> Roles { get; set; } = new();
    public List<PermissionDto> Permissions { get; set; } = new();
}
