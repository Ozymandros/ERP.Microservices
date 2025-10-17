using Microsoft.AspNetCore.Mvc;
using MyApp.Auth.Application.Contracts;

[ApiController]
[Route("permissions")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public PermissionsController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    //[HttpGet("check")]
    //public async Task<IActionResult> CheckPermission(Guid userId, string module, string action)
    //{
    //    var hasPermission = await _permissionService.HasPermissionAsync(userId, module, action);
    //    return Ok(hasPermission);
    //}

    [HttpGet("check")]
    public async Task<IActionResult> CheckPermission(string? username, string module, string action)
    {
        var hasPermission = await _permissionService.HasPermissionAsync(username, module, action);
        return Ok(hasPermission);
    }
}