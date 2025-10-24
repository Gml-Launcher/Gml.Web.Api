using System;
using System.Collections.Generic;

namespace Gml.Web.Api.Domains.Auth;

public class ExternalApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int UserId { get; set; }
    public string Name { get; set; } = null!;
    public string TokenHash { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ApplicationPermission> ApplicationPermissions { get; set; } = new List<ApplicationPermission>();
}
