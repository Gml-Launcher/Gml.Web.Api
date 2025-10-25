using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Integrations.Auth;

public class NamelessMCAuthService(IHttpClientFactory httpClientFactory, IGmlManager gmlManager)
    : CustomEndpointAuthService(httpClientFactory, gmlManager)
{

}
