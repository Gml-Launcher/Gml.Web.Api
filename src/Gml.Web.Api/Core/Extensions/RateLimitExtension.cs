using System.Threading.RateLimiting;

namespace Gml.Web.Api.Core.Extensions;

public static class RateLimitExtension
{
    public static IServiceCollection ConfigureRateLimit(this IServiceCollection services)
    {
        // services.AddRateLimiter(options =>
        // {
        //     options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        //         RateLimitPartition.GetFixedWindowLimiter(
        //             partitionKey: context.User.Identity?.Name ??
        //                           context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        //             factory: _ => new FixedWindowRateLimiterOptions
        //             {
        //                 PermitLimit = 100,
        //                 Window = TimeSpan.FromMinutes(1),
        //                 QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        //                 QueueLimit = 100
        //             }));
        // });

        return services;
    }
}
