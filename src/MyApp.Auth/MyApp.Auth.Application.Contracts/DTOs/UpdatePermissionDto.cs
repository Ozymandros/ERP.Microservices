namespace MyApp.Auth.Application.Contracts.DTOs
{
    /// <summary>
    /// Data transfer object for updating an existing permission.
    /// </summary>
    public record UpdatePermissionDto(
        string Module,
        string Action,
        string? Description = null
    );
}
