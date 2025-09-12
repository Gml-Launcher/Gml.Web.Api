namespace Gml.Web.Api.Domains.Auth;

public class UserRole
{
    public int UserId { get; set; }
    public Gml.Web.Api.Domains.User.User User { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
