using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Integrations.Auth;

public class WebMCRAuthService(IHttpClientFactory httpClientFactory, IGmlManager gmlManager)
    : CustomEndpointAuthService(httpClientFactory, gmlManager)
{

}
