namespace MyApp.Shared.Domain.Permissions;

/// <summary>
/// Provides permission checking functionality for module and action combinations.
/// </summary>
public interface IPermissionChecker
{
    /// <summary>
    /// Checks if a specific user has permission for a module and action.
    /// </summary>
    Task<bool> HasPermissionAsync(Guid userId, string module, string action);

    /// <summary>
    /// Checks if the current user has permission for a module and action.
    /// </summary>
    Task<bool> HasPermissionAsync(string module, string action);
}