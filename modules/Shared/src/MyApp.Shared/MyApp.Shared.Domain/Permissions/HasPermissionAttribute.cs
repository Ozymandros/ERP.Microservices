﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

public class HasPermissionAttribute : AuthorizeAttribute, IAsyncAuthorizationFilter
{
    private readonly string _module;
    private readonly string _action;

    public HasPermissionAttribute(string module, string action)
    {
        _module = module;
        _action = action;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // 1. Verifica que l'usuari està autenticat
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated is not true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // 2. Obté el username i els roles
        var username = user.FindFirst(ClaimTypes.Name)?.Value;
        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        // 3. Invoca el servei de permisos (Dapr, API, etc.)
        var permissionChecker = context.HttpContext.RequestServices.GetRequiredService<IPermissionChecker>();
        //var hasPermission = await permissionChecker.HasPermissionAsync(Guid.Parse(userId), _module, _action);
        bool hasPermission = await permissionChecker.HasPermissionAsync(_module, _action);

        // 4. Si no té permisos, denega l'accés
        if (!hasPermission)
        {
            context.Result = new ForbidResult();
        }
    }
}
