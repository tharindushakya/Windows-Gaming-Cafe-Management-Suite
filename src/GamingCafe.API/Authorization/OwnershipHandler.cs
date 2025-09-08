using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GamingCafe.Core.Authorization;

namespace GamingCafe.API.Authorization;

public class OwnershipHandler : AuthorizationHandler<OwnershipRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OwnershipRequirement requirement)
    {
        // If user is admin, succeed
        if (context.User?.IsInRole("Admin") == true)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Example: check for a claim 'ownerId' that must match 'subject' name identifier
        var ownerClaim = context.User?.FindFirst("ownerId")?.Value;
        var subject = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(ownerClaim) && !string.IsNullOrEmpty(subject) && ownerClaim == subject)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
