using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Integrations.Auth;

public class WordPressAuthService(IHttpClientFactory httpClientFactory, IGmlManager gmlManager)
    : CustomEndpointAuthService(httpClientFactory, gmlManager)
{

}
