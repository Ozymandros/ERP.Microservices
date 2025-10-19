using Microsoft.AspNetCore.Identity;

public class Permission
{
    public Guid Id { get; set; }
    public string Module { get; set; } = default!;
    public string Action { get; set; } = default!;
    public string? Description { get; set; }
}

