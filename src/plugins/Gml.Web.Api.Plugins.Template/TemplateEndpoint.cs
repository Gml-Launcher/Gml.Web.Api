using System;
using System.Threading.Tasks;
using Gml.Web.Api.EndpointSDK;
using GmlCore.Interfaces;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Gml.Web.Api.Plugins.Template;

[Path("get", "/api/v1/plugins/template", true)]
public class TemplateEndpoint : IPluginEndpoint
{
    public async Task Execute(HttpContext context, IGmlManager gmlManager, IServiceProvider serviceProvider)
    {
        var profiles = await gmlManager.Profiles.GetProfiles();
        var endpoint = new EndpointHelper();

        await endpoint.Ok(context, profiles, "Супер");
    }
}
