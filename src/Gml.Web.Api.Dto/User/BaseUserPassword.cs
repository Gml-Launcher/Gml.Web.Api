namespace Gml.Web.Api.Dto.User;

using Newtonsoft.Json;
using System.Text.Json.Serialization;

public class BaseUserPassword
{
    public string Login { get; set; }
    public string Password { get; set; }
    public string AccessToken { get; set; }
    [JsonProperty("2FACode")]
    [JsonPropertyName("2FACode")]
    public string? TwoFactorCode { get; set; }
}
