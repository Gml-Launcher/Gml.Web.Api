#nullable enable
namespace Gml.Web.Api.Domains.Integrations;

public class AuthResult
{
    public bool IsSuccess { get; set; }
    public string? Login { get; set; }
    public string? Uuid { get; set; }
    public string? Message { get; set; }
    public bool IsSlim { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }
    public string? TwoFactorSecretTemp { get; set; }
}
