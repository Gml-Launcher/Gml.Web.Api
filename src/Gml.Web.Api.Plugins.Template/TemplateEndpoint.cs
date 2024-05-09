using System.Threading.Tasks;
using Gml.Web.Api.EndpointSDK;
using GmlCore.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Gml.Web.Api.Plugins.Template;

[Path("get", "/api/v1/plugins/template", true)]
public class TemplateEndpoint : IPluginEndpoint
{
    public async Task Execute(HttpContext context, IGmlManager gmlManager)
    {
        await context.Response.WriteAsync("templaацуфацфуаte");
    }
}
