namespace MyApp.Auth.Application.Contracts.DTOs
{
    public record UpdatePermissionDto(
        string Module,
        string Action,
        string? Description = null
    );
}
