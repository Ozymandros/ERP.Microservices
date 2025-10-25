namespace MyApp.Auth.Application.Contracts.DTOs
{
    public class PermissionDto
    {
        public Guid Id { get; set; }
        public string Module { get; set; } = default!;
        public string Action { get; set; } = default!;
        public string? Description { get; set; }
    }
}