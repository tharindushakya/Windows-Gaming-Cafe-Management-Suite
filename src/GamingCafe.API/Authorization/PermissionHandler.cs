using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GamingCafe.Core.Interfaces.Services;

namespace GamingCafe.API.Authorization
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IUserService _userService;

        public PermissionHandler(IUserService userService)
        {
            _userService = userService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            // If user has a permission claim, respect it immediately
            if (context.User.HasClaim(c => c.Type == GamingCafe.Core.Authorization.CustomClaimTypes.Permission && c.Value == requirement.Permission))
            {
                context.Succeed(requirement);
                return;
            }

            // Otherwise attempt to resolve user id and ask IUserService for permission
            var idClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out var userId))
            {
                var has = await _userService.HasPermissionAsync(userId, requirement.Permission);
                if (has)
                {
                    context.Succeed(requirement);
                    return;
                }
            }

            // Not authorized for this permission
            context.Fail();
        }
    }
}
