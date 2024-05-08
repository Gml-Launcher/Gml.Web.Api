using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Gml.Web.Api.EndpointSDK;

namespace Gml.Web.Api.Core.Middlewares;

public class PluginMiddleware
{
    private readonly RequestDelegate _next;

    public PluginMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var reference = await Process(context);

        if (!context.Response.HasStarted)
        {
            await _next(context);
        }

        for (int i = 0; i < 10 && reference.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Console.WriteLine($"Unload successful: {!reference.IsAlive}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference> Process(HttpContext context)
    {
        var directoryInfo = new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins"));

        if (!directoryInfo.Exists)
        {
            directoryInfo.Create();
        }

        var plugins = directoryInfo.GetFiles("*.dll");

        var loadContext = new AssemblyLoadContext("GmlAssemblyResolver", true);

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

                    var endpoint = Activator.CreateInstance(type) as IPluginEndpoint;
                    await endpoint?.Execute(context)!;
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
