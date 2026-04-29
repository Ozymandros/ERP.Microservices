using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace MyApp.Shared.Domain.Permissions;

/// <summary>
/// Authorization filter attribute that checks user permissions for specific module and action combinations.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class HasPermissionAttribute : AuthorizeAttribute, IAsyncAuthorizationFilter
{
    private readonly string _module;
    private readonly string _action;

    /// <summary>
    /// Initializes a new instance of the HasPermissionAttribute class.
    /// </summary>
    public HasPermissionAttribute(string module, string action)
    {
        _module = module;
        _action = action;
    }

    /// <summary>
    /// Asynchronously checks if the user has permission for the specified module and action.
    /// </summary>
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // 1. Verify that the user is authenticated
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated is not true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // 2. Get the username and roles
        var username = user.FindFirst(ClaimTypes.Name)?.Value;
        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        // 3. Invoke the permissions service (Dapr, API, etc.)
        var permissionChecker = context.HttpContext.RequestServices.GetRequiredService<IPermissionChecker>();
        bool hasPermission = await permissionChecker.HasPermissionAsync(_module, _action);

        // 4. If no permissions, deny access
        if (!hasPermission)
        {
            context.Result = new ForbidResult();
        }
    }
}
