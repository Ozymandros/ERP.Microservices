using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Domain.Entities;

namespace MyApp.Auth.Application.Tests.Builders;

public class ApplicationRoleBuilder
{
    private ApplicationRole _role = new("TestRole")
    {
        Id = Guid.NewGuid(),
        Description = "Test role description",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsActive = true
    };

    public ApplicationRoleBuilder WithId(Guid id)
    {
        _role.Id = id;
        return this;
    }

    public ApplicationRoleBuilder WithName(string name)
    {
        _role.Name = name;
        return this;
    }

    public ApplicationRoleBuilder WithDescription(string description)
    {
        _role.Description = description;
        return this;
    }

    public ApplicationRoleBuilder WithCreatedAt(DateTime createdAt)
    {
        _role.CreatedAt = createdAt;
        return this;
    }

    public ApplicationRoleBuilder WithUpdatedAt(DateTime updatedAt)
    {
        _role.UpdatedAt = updatedAt;
        return this;
    }

    public ApplicationRoleBuilder WithIsActive(bool isActive)
    {
        _role.IsActive = isActive;
        return this;
    }

    public ApplicationRole Build() => _role;

    public static implicit operator ApplicationRole(ApplicationRoleBuilder builder) => builder.Build();
}

public class RoleBuilder
{
    private ApplicationRole _role = new("TestRole")
    {
        Id = Guid.NewGuid(),
        Description = "Test role description",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsActive = true
    };

    public RoleBuilder WithId(Guid id)
    {
        _role.Id = id;
        return this;
    }

    public RoleBuilder WithName(string name)
    {
        _role.Name = name;
        return this;
    }

    public RoleBuilder WithDescription(string description)
    {
        _role.Description = description;
        return this;
    }

    public RoleBuilder WithCreatedAt(DateTime createdAt)
    {
        _role.CreatedAt = createdAt;
        return this;
    }

    public RoleBuilder WithUpdatedAt(DateTime updatedAt)
    {
        _role.UpdatedAt = updatedAt;
        return this;
    }

    public ApplicationRole Build() => _role;

    public static implicit operator ApplicationRole(RoleBuilder builder) => builder.Build();
}

public class RoleDtoBuilder
{
    private Guid _id = Guid.NewGuid();
    private DateTime _createdAt = DateTime.UtcNow;
    private string _createdBy = "TestUser";
    private DateTime? _updatedAt = null;
    private string? _updatedBy = null;
    private string? _name = "TestRole";
    private string? _description = "Test role description";

    public RoleDtoBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public RoleDtoBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public RoleDtoBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public RoleDto Build() => new(_id, _createdAt, _createdBy, _updatedAt, _updatedBy, _name, _description);

    public static implicit operator RoleDto(RoleDtoBuilder builder) => builder.Build();
}

public class CreateRoleDtoBuilder
{
    private string _name = "NewRole";
    private string? _description = "New role description";

    public CreateRoleDtoBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CreateRoleDtoBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public CreateRoleDto Build() => new(_name, _description);

    public static implicit operator CreateRoleDto(CreateRoleDtoBuilder builder) => builder.Build();
}

public class UpdateRoleDtoBuilder
{
    private string _name = "UpdatedRole";
    private string? _description = "Updated role description";

    public UpdateRoleDtoBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public UpdateRoleDtoBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public CreateRoleDto Build() => new(_name, _description);

    public static implicit operator CreateRoleDto(UpdateRoleDtoBuilder builder) => builder.Build();
}