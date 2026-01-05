using Microsoft.AspNetCore.Identity;
using MyApp.Shared.Domain.Entities;

public class Permission(Guid id) : AuditableEntity<Guid>(id)
{
    public required string Module { get; set; }
    public required string Action { get; set; }
    public string? Description { get; set; }
}

public class PermissionComparer : IEqualityComparer<Permission>
{
    public bool Equals(Permission? x, Permission? y)
        => x?.Id == y?.Id;

    public int GetHashCode(Permission obj)
        => obj.Id.GetHashCode();
}

