using System.Reflection;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.EndpointSDK;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Middlewares;

public class PluginRouterMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (PluginRouter.TryGetEndpoint(context.Request.Method, context.Request.Path, out var endpoint))
        {
            if (endpoint.GetType().GetCustomAttributes(typeof(PathAttribute)).FirstOrDefault() is PathAttribute
                {
                    NeedAuth: true
                } && context.User.Identity?.IsAuthenticated == false)
            {
                context.Response.StatusCode = 401;
                return;
            }

            var gmlManager = context.RequestServices.GetRequiredService<IGmlManager>();
            await endpoint.Execute(context, gmlManager, context.RequestServices);
            return;
        }

        await next(context);
    }
}

