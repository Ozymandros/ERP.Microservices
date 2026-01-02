namespace MyApp.Auth.Application.Contracts.DTOs
{
    public record CreatePermissionDto(
        string Module,
        string Action,
        string? Description = null
    );
}
