using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Gml.Web.Api.Core.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        // Admin bypass
        if (context.User.IsInRole("Admin") || context.User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check permission claim
        var hasPerm = context.User.Claims.Any(c => c.Type == "perm" && string.Equals(c.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase));
        if (hasPerm)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
