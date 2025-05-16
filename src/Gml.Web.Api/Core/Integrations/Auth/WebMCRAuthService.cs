using System.Text;
using Gml.Web.Api.Domains.Integrations;
using GmlCore.Interfaces;
using Newtonsoft.Json;

namespace Gml.Web.Api.Core.Integrations.Auth;

public class WebMCRAuthService(IHttpClientFactory httpClientFactory, IGmlManager gmlManager)
    : CustomEndpointAuthService(httpClientFactory, gmlManager)
{

}
