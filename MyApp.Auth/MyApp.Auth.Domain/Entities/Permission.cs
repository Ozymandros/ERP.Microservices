using Microsoft.AspNetCore.Identity;
using MyApp.Shared.Domain.Entities;

public class Permission : AuditableEntity<Guid>
{
    public string Module { get; set; } = default!;
    public string Action { get; set; } = default!;
    public string? Description { get; set; }
}

public class PermissionComparer : IEqualityComparer<Permission>
{
    public bool Equals(Permission x, Permission y)
        => x?.Id == y?.Id; // o qualsevol camp únic que defineixi la igualtat

    public int GetHashCode(Permission obj)
        => obj.Id.GetHashCode();
}

