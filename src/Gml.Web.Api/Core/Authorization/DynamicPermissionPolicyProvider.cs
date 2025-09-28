using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Gml.Web.Api.Core.Authorization;

public class DynamicPermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public const string Prefix = "perm:";

    public DynamicPermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallbackPolicyProvider.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName != null && policyName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            var perm = policyName.Substring(Prefix.Length);
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(perm))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}
