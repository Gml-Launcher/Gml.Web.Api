using System;
using System.Threading.Tasks;
using Gml.Web.Api.EndpointSDK;
using Gml.Web.Api.Plugin.Avanguard.Core;
using GmlCore.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Gml.Web.Api.Plugin.Avanguard;

[Path("post", "/api/v1/plugins/avanguard/compile", true)]
public class GravitGuardEndpoint : IPluginEndpoint
{
    public Task Execute(HttpContext context, IGmlManager gmlManager, IServiceProvider serviceProvider)
    {
        throw new NotImplementedException();
    }
}
