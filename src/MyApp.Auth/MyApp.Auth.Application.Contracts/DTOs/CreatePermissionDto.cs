namespace MyApp.Auth.Application.Contracts.DTOs
{
    public record CreatePermissionDto
    {
        public CreatePermissionDto() { }
        
        public CreatePermissionDto(
            string module,
            string action,
            string? description = null)
        {
            Module = module;
            Action = action;
            Description = description;
        }
        
        public string Module { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
