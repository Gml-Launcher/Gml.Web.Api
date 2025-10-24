using System;
using System.Collections.Generic;

namespace Gml.Web.Api.Dto.Auth;

public class ExternalApplicationReadDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Token { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public List<PermissionDto> Permissions { get; set; } = new();
}
