namespace MyApp.Auth.Application.Contracts.DTOs
{
    /// <summary>
    /// Data transfer object for creating a new permission.
    /// </summary>
    public record CreatePermissionDto(
        string Module,
        string Action,
        string? Description = null
    );
}
