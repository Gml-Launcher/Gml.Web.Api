using System.Net;
using Gml.Web.Api.Core.Options;

namespace Gml.Web.Api.Core.Extensions;

public static class HttpClientsExtensions
{
    public static IServiceCollection AddNamedHttpClients(
        this IServiceCollection services,
        string marketEndpoint)
    {
        string? skinsServiceUrl = Environment.GetEnvironmentVariable("SkinServiceUrl");

        var dockerEnv = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");

        var isRunningInDocker = !string.IsNullOrEmpty(dockerEnv) && dockerEnv.ToLower() == "true";

        services.AddHttpClient(HttpClientNames.SkinService, client =>
        {
            client.BaseAddress = isRunningInDocker
                ? new Uri("http://gml-web-skins:8085/")
                : string.IsNullOrEmpty(skinsServiceUrl)
                    ? null
                    : new Uri(skinsServiceUrl);
        });

        services.AddHttpClient(HttpClientNames.MarketService,
            client => { client.BaseAddress = new Uri(marketEndpoint); });

        return services;
    }

    public static IPAddress? ParseRemoteAddress(this HttpContext context)
    {
        var headers = context.Request.Headers;

        foreach (var headerName in new[] { "CF-Connecting-IP", "True-Client-IP", "X-Real-IP" })
        {
            if (headers.TryGetValue(headerName, out var values))
            {
                var candidate = values.FirstOrDefault();
                if (TryParseIp(candidate, out var ip))
                    return ip;
            }
        }

        if (headers.TryGetValue("Forwarded", out var forwarded))
        {
            var firstForwarded = forwarded.ToString().Split(',')[0];
            foreach (var part in firstForwarded.Split(';',
                         StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (part.StartsWith("for=", StringComparison.OrdinalIgnoreCase))
                {
                    var value = part.Substring(4);
                    if (TryParseIp(value, out var ip))
                        return ip;
                }
            }
        }

        if (headers.TryGetValue("X-Forwarded-For", out var xff) && !string.IsNullOrWhiteSpace(xff))
        {
            foreach (var raw in xff.ToString()
                         .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (TryParseIp(raw, out var ip))
                    return ip;
            }
        }

        return context.Connection.RemoteIpAddress;
    }

    private static bool TryParseIp(string? value, out IPAddress ip)
    {
        ip = IPAddress.None;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var s = value.Trim().Trim('"');
        if (s.Equals("unknown", StringComparison.OrdinalIgnoreCase))
            return false;

        if (s.StartsWith("[") && s.Contains(']'))
        {
            var end = s.IndexOf(']');
            if (end > 1)
                s = s.Substring(1, end - 1);
        }
        else
        {
            var firstColon = s.IndexOf(':');
            if (firstColon > 0 && firstColon == s.LastIndexOf(':'))
            {
                s = s.Substring(0, firstColon);
            }
        }

        return IPAddress.TryParse(s, out ip);
    }
}
