namespace Gml.Web.Api.Core.Integrations.Auth;

public interface IPlatformAuthService
{
    Task<bool> Auth(string login, string password);
}
