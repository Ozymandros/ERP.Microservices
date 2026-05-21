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

public class HasPermissionAttribute : Attribute, IAsyncAuthorizationFilter

{

    /// <summary>JWT claim type for module:action permission entries.</summary>

    public const string PermissionClaimType = "permission";



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

        var user = context.HttpContext.User;



        if (user.Identity?.IsAuthenticated is not true)

        {

            context.Result = new UnauthorizedResult();

            return;

        }



        if (user.IsInRole("Admin"))

            return;



        var requiredPermission = $"{_module}:{_action}";

        var permissionClaims = user.FindAll(PermissionClaimType).ToList();



        if (permissionClaims.Count > 0)

        {

            var allowed = permissionClaims.Any(c =>

                string.Equals(c.Value, requiredPermission, StringComparison.OrdinalIgnoreCase));

            if (!allowed)

                context.Result = new ForbidResult();

            return;

        }



        // Legacy tokens without permission claims: fall back to Auth via Dapr

        var permissionChecker = context.HttpContext.RequestServices.GetRequiredService<IPermissionChecker>();

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value

            ?? user.FindFirst("sub")?.Value;



        bool hasPermission;

        if (Guid.TryParse(userIdClaim, out var userId))

            hasPermission = await permissionChecker.HasPermissionAsync(userId, _module, _action);

        else

            hasPermission = await permissionChecker.HasPermissionAsync(_module, _action);



        if (!hasPermission)

            context.Result = new ForbidResult();

    }

}


