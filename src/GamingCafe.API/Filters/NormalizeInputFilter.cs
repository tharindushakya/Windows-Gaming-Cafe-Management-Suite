using System.Reflection;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GamingCafe.API.Filters;

/// <summary>
/// Action filter that normalizes string properties on action arguments: trims and optionally lower-cases emails/usernames.
/// Applied globally so controllers don't need to mutate request objects.
/// </summary>
public class NormalizeInputFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var arg in context.ActionArguments.Values)
        {
            if (arg == null) continue;
            var type = arg.GetType();
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.PropertyType != typeof(string) || !prop.CanRead || !prop.CanWrite) continue;
                try
                {
                    var val = (string?)prop.GetValue(arg);
                    // Coalesce null to empty so controllers can rely on non-nullable model fields
                    var normalized = (val ?? string.Empty).Trim();
                    // heuristic: normalize emails and usernames to lower-case
                    if (prop.Name.ToLowerInvariant().Contains("email") || prop.Name.ToLowerInvariant().Contains("username"))
                        normalized = normalized.ToLowerInvariant();
                    prop.SetValue(arg, normalized);
                }
                catch
                {
                    // ignore normalization errors
                }
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // no-op
    }
}
