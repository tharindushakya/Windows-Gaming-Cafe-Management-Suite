using System.Net;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authorization;
using GamingCafe.Core.Authorization;

namespace GamingCafe.API.Filters;

public class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext == null) return false;

        // Allow local requests
        var ip = httpContext.Connection.RemoteIpAddress;
        if (ip != null && (IPAddress.IsLoopback(ip) || ip.ToString() == "::1"))
            return true;

        // Otherwise prefer the IAuthorizationService and the RequireAdmin policy
        var authz = httpContext.RequestServices.GetService<IAuthorizationService>();
        if (authz != null && httpContext.User?.Identity?.IsAuthenticated == true)
        {
            var authResult = authz.AuthorizeAsync(httpContext.User, null, PolicyNames.RequireAdmin).GetAwaiter().GetResult();
            return authResult.Succeeded;
        }

        // Fallback to role checks if Authorization service isn't registered (non-breaking)
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            if (httpContext.User.IsInRole("Administrator") || httpContext.User.IsInRole("Admin"))
                return true;
        }

        return false;
    }
}
