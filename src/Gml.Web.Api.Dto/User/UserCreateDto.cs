namespace Gml.Web.Api.Dto.User;

public class UserCreateDto : BaseUserPassword
{
    public string Email { get; set; }
    public string? Role { get; set; }
}
