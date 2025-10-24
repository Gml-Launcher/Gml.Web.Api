using System;

namespace Gml.Web.Api.Domains.Auth;

public class ApplicationPermission
{
    public Guid ApplicationId { get; set; }
    public ExternalApplication Application { get; set; } = null!;

    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}
