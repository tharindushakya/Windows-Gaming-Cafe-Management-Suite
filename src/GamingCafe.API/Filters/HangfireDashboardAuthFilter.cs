using System.Net;
using Hangfire.Dashboard;

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

        // Otherwise require an authenticated admin user
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            if (httpContext.User.IsInRole("Administrator") || httpContext.User.IsInRole("Admin"))
                return true;
        }

        return false;
    }
}
