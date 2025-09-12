using System.Collections.Generic;

namespace Gml.Web.Api.Domains.Auth;

public class Permission
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
