namespace Gml.Web.Api.Dto.Auth;

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}
