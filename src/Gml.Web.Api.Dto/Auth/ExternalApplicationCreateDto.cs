using System.Collections.Generic;

namespace Gml.Web.Api.Dto.Auth;

public class ExternalApplicationCreateDto
{
    public string Name { get; set; } = null!;
    public List<int> PermissionIds { get; set; } = new();
}
