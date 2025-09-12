namespace Gml.Web.Api.Dto.Auth;

public class AuthTokensDto
{
    public string AccessToken { get; set; } = null!;
    public int ExpiresIn { get; set; }
    public string RefreshToken { get; set; } = null!;
}