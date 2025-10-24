using System;
using System.Collections.Generic;

namespace Gml.Web.Api.Dto.Auth;

public class ExternalApplicationListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public List<PermissionDto> Permissions { get; set; } = new();
}
