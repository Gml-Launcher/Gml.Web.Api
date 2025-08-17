using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using Gml.Web.Api.EndpointSDK;
using GmlCore.Interfaces;
using System.Diagnostics;

namespace Gml.Web.Api.Core.Services;

public class PluginAssemblyManager
{
    private readonly Dictionary<string, PluginHandle> _plugins = new();

    public void LoadPlugin(string pathToDll)
    {
        try
        {
            var context = new PluginLoadContext(pathToDll);
            var assembly = context.LoadFromAssemblyName(AssemblyName.GetAssemblyName(pathToDll));

            foreach (var type in assembly.GetTypes())
            {
                if (typeof(IPluginEndpoint).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    var attr = type.GetCustomAttribute<PathAttribute>();
                    if (attr != null)
                    {
                        var endpoint = (IPluginEndpoint)Activator.CreateInstance(type)!;
                        PluginRouter.RegisterEndpoint(attr.Method!, attr.Path!, attr.NeedAuth, endpoint);
                    }
                }
            }

            _plugins[pathToDll] = new PluginHandle(context, assembly);
        }
        catch (Exception exception)
        {
            SentrySdk.CaptureException(exception);
        }
    }

    public void UnloadPlugin(string pathToDll)
    {
        if (_plugins.TryGetValue(pathToDll, out var handle))
        {
            // Сначала отключаем все эндпоинты из этой сборки
            if (handle.AssemblyReference.TryGetTarget(out var assembly))
            {
                PluginRouter.UnregisterEndpointsFromAssembly(assembly);
            }

            // Удаляем плагин из словаря перед выгрузкой, чтобы убрать ссылку
            _plugins.Remove(pathToDll);

            // Принудительно вызываем сборку мусора перед выгрузкой
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Вызываем Unload один раз - многократные вызовы не помогают
            handle.Context.Unload();

            // Еще раз запускаем сборку мусора после выгрузки
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    private class PluginHandle
    {
        public PluginLoadContext Context { get; }
        public WeakReference<Assembly> AssemblyReference { get; }

        public PluginHandle(PluginLoadContext context, Assembly assembly)
        {
            Context = context;
            AssemblyReference = new WeakReference<Assembly>(assembly);
        }

        public bool TryGetAssembly(out Assembly? assembly)
        {
            return AssemblyReference.TryGetTarget(out assembly);
        }
    }
}

public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path != null ? LoadFromAssemblyPath(path) : null;
    }
}

public static class PluginRouter
{
    private static readonly ConcurrentDictionary<(string method, string path), (bool needAuth, IPluginEndpoint endpoint)> _routes = new();
    private static readonly ConcurrentDictionary<Assembly, List<(string method, string path)>> _assemblyRoutes = new();

    public static void RegisterEndpoint(string method, string path, bool needAuth, IPluginEndpoint endpoint)
    {
        var key = (method.ToUpperInvariant(), path);
        _routes[key] = (needAuth, endpoint);

        var asm = endpoint.GetType().Assembly;
        if (!_assemblyRoutes.TryGetValue(asm, out var list))
        {
            list = new List<(string, string)>();
            _assemblyRoutes[asm] = list;
        }
        list.Add(key);
    }

    // public static void UnregisterEndpointsFromAssembly(Assembly assembly)
    // {
    //     if (_assemblyRoutes.TryGetValue(assembly, out var list))
    //     {
    //         foreach (var key in list)
    //             _routes.TryRemove(key, out _);
    //
    //         _assemblyRoutes.TryRemove(assembly, out _);
    //     }
    // }

    public static void UnregisterEndpointsFromAssembly(Assembly assembly)
    {
        if (_assemblyRoutes.TryGetValue(assembly, out var list))
        {
            // Создаем копию списка для итерации, чтобы избежать проблем с коллекцией
            var routesToRemove = list.ToList();

            // Удаляем все маршруты
            foreach (var key in routesToRemove)
            {
                if (_routes.TryRemove(key, out var routeData))
                {
                    // Обнуляем ссылки на endpoint для помощи сборщику мусора
                    // Это критически важно для освобождения ссылок на сборку
                    var fieldInfo = routeData.endpoint.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    foreach (var field in fieldInfo)
                    {
                        try
                        {
                            if (!field.IsInitOnly)
                                field.SetValue(routeData.endpoint, null);
                        }
                        catch
                        {
                            // Игнорируем ошибки при попытке очистки полей
                        }
                    }
                }
            }

            // Удаляем ассоциацию сборки с маршрутами
            _assemblyRoutes.TryRemove(assembly, out _);

            // Принудительно запускаем сборку мусора
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    public static bool TryGetEndpoint(string method, PathString path, out IPluginEndpoint endpoint)
    {
        var key = (method.ToUpperInvariant(), path.Value ?? "");
        if (_routes.TryGetValue(key, out var value))
        {
            endpoint = value.endpoint;
            return true;
        }

        endpoint = null!;
        return false;
    }
}
