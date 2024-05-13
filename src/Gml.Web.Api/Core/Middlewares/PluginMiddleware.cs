using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Gml.Web.Api.Core.Services;
using Gml.Web.Api.EndpointSDK;
using GmlCore.Interfaces;

namespace Gml.Web.Api.Core.Middlewares;

public class PluginMiddleware
{
    private readonly RequestDelegate _next;
    private static AccessTokenService _accessTokenService;
    private static IGmlManager _gmlManager;

    public PluginMiddleware(RequestDelegate next, AccessTokenService accessTokenService, IGmlManager gmlManager)
    {
        _next = next;
        _accessTokenService = accessTokenService;
        _gmlManager = gmlManager;
    }

    public async Task Invoke(HttpContext context)
    {
        var reference = await Process(context);

        if (reference is null)
        {
            return;
        }

        if (!context.Response.HasStarted)
        {
            await _next(context);
        }

        for (int i = 0; i < 10 && reference.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Debug.WriteLine($"Unload successful: {!reference.IsAlive}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference?> Process(HttpContext context)
    {
        var loadContext = new AssemblyLoadContext("GmlAssemblyResolver", true);

        if (string.IsNullOrEmpty(context.Request.Path.Value) || !context.Request.Path.Value.Contains("plugins"))
            return new WeakReference(loadContext);

        var directoryInfo = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins"));

        if (!directoryInfo.Exists)
        {
            directoryInfo.Create();
        }

        var plugins = directoryInfo.GetFiles("*.dll");


        try
        {
            foreach (var plugin in plugins)
            {
                var assembly = loadContext.LoadFromAssemblyPath(plugin.FullName);

                foreach (var type in assembly.GetTypes())
                {
                    if (!typeof(IPluginEndpoint).IsAssignableFrom(type)) continue;

                    var pathInfo = type.GetCustomAttribute<PathAttribute>();

                    if (pathInfo is not { Method: not null, Path: not null }
                        || !pathInfo.Method.Equals(context.Request.Method, StringComparison.OrdinalIgnoreCase)
                        || !pathInfo.Path.Equals(context.Request.Path, StringComparison.OrdinalIgnoreCase)) continue;

                    if (pathInfo.NeedAuth)
                    {
                        var accessToken = context.Request.Headers.Authorization
                            .FirstOrDefault()
                            ?.Split("Bearer ")
                            .Last();

                        if (string.IsNullOrEmpty(accessToken)|| !_accessTokenService.ValidateToken(accessToken))
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return null;
                        }


                    }

                    var endpoint = Activator.CreateInstance(type) as IPluginEndpoint;
                    await endpoint?.Execute(context, _gmlManager)!;
                }
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
        finally
        {
            loadContext.Unload();
        }

        return new WeakReference(loadContext);
    }

}
