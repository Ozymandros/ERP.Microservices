namespace MyApp.Auth.Application.Contracts.DTOs
{
    /// <summary>
    /// Updates the permission dto.
    /// </summary>
    /// <param name="Module">The module.</param>
    /// <param name="Action">The action.</param>
    /// <param name="Description">The description.</param>
    public record UpdatePermissionDto(
        string Module,
        string Action,
        string? Description = null
    );
}
