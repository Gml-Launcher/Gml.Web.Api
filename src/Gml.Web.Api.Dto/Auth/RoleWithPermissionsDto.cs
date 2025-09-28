using System.Collections.Generic;

namespace Gml.Web.Api.Dto.Auth;

public class RoleWithPermissionsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public List<PermissionDto> Permissions { get; set; } = new();
}
