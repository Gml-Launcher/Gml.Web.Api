using Gml.Web.Api.Core.Services;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Middlewares;

public class PluginRouterMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (PluginRouter.TryGetEndpoint(context.Request.Method, context.Request.Path, out var endpoint))
        {
            var gmlManager = context.RequestServices.GetRequiredService<IGmlManager>();
            await endpoint.Execute(context, gmlManager);
            return;
        }

        await next(context);
    }
}

