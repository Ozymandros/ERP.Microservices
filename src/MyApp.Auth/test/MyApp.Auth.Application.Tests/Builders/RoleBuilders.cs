using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Domain.Entities;

namespace MyApp.Auth.Application.Tests.Builders;

/// <summary>
/// Builder for constructing <see cref="ApplicationRole"/> test fixtures using a fluent API.
/// </summary>
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

    /// <summary>
    /// Sets the role's unique identifier.
    /// </summary>
    /// <param name="id">The identifier to assign.</param>
    /// <returns>The current <see cref="ApplicationRoleBuilder"/> instance for chaining.</returns>
    public ApplicationRoleBuilder WithId(Guid id)
    {
        _role.Id = id;
        return this;
    }

    /// <summary>
    /// Sets the role's name.
    /// </summary>
    /// <param name="name">The role name to assign.</param>
    /// <returns>The current <see cref="ApplicationRoleBuilder"/> instance for chaining.</returns>
    public ApplicationRoleBuilder WithName(string name)
    {
        _role.Name = name;
        return this;
    }

    /// <summary>
    /// Sets the role's description.
    /// </summary>
    /// <param name="description">The description to assign.</param>
    /// <returns>The current <see cref="ApplicationRoleBuilder"/> instance for chaining.</returns>
    public ApplicationRoleBuilder WithDescription(string description)
    {
        _role.Description = description;
        return this;
    }

    /// <summary>
    /// Sets the timestamp at which the role was created.
    /// </summary>
    /// <param name="createdAt">The creation timestamp to assign.</param>
    /// <returns>The current <see cref="ApplicationRoleBuilder"/> instance for chaining.</returns>
    public ApplicationRoleBuilder WithCreatedAt(DateTime createdAt)
    {
        _role.CreatedAt = createdAt;
        return this;
    }

    /// <summary>
    /// Sets the timestamp at which the role was last updated.
    /// </summary>
    /// <param name="updatedAt">The last-update timestamp to assign.</param>
    /// <returns>The current <see cref="ApplicationRoleBuilder"/> instance for chaining.</returns>
    public ApplicationRoleBuilder WithUpdatedAt(DateTime updatedAt)
    {
        _role.UpdatedAt = updatedAt;
        return this;
    }

    /// <summary>
    /// Sets whether the role is active.
    /// </summary>
    /// <param name="isActive">Whether the role is active.</param>
    /// <returns>The current <see cref="ApplicationRoleBuilder"/> instance for chaining.</returns>
    public ApplicationRoleBuilder WithIsActive(bool isActive)
    {
        _role.IsActive = isActive;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured <see cref="ApplicationRole"/> instance.
    /// </summary>
    /// <returns>The constructed <see cref="ApplicationRole"/>.</returns>
    public ApplicationRole Build() => _role;

    /// <summary>
    /// Implicitly converts an <see cref="ApplicationRoleBuilder"/> to an <see cref="ApplicationRole"/>.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The constructed <see cref="ApplicationRole"/>.</returns>
    public static implicit operator ApplicationRole(ApplicationRoleBuilder builder) => builder.Build();
}

/// <summary>
/// Builder for constructing <see cref="ApplicationRole"/> test fixtures (alias for <see cref="ApplicationRoleBuilder"/>).
/// </summary>
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

    /// <summary>
    /// Sets the role's unique identifier.
    /// </summary>
    /// <param name="id">The identifier to assign.</param>
    /// <returns>The current <see cref="RoleBuilder"/> instance for chaining.</returns>
    public RoleBuilder WithId(Guid id)
    {
        _role.Id = id;
        return this;
    }

    /// <summary>
    /// Sets the role's name.
    /// </summary>
    /// <param name="name">The role name to assign.</param>
    /// <returns>The current <see cref="RoleBuilder"/> instance for chaining.</returns>
    public RoleBuilder WithName(string name)
    {
        _role.Name = name;
        return this;
    }

    /// <summary>
    /// Sets the role's description.
    /// </summary>
    /// <param name="description">The description to assign.</param>
    /// <returns>The current <see cref="RoleBuilder"/> instance for chaining.</returns>
    public RoleBuilder WithDescription(string description)
    {
        _role.Description = description;
        return this;
    }

    /// <summary>
    /// Sets the timestamp at which the role was created.
    /// </summary>
    /// <param name="createdAt">The creation timestamp to assign.</param>
    /// <returns>The current <see cref="RoleBuilder"/> instance for chaining.</returns>
    public RoleBuilder WithCreatedAt(DateTime createdAt)
    {
        _role.CreatedAt = createdAt;
        return this;
    }

    /// <summary>
    /// Sets the timestamp at which the role was last updated.
    /// </summary>
    /// <param name="updatedAt">The last-update timestamp to assign.</param>
    /// <returns>The current <see cref="RoleBuilder"/> instance for chaining.</returns>
    public RoleBuilder WithUpdatedAt(DateTime updatedAt)
    {
        _role.UpdatedAt = updatedAt;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured <see cref="ApplicationRole"/> instance.
    /// </summary>
    /// <returns>The constructed <see cref="ApplicationRole"/>.</returns>
    public ApplicationRole Build() => _role;

    /// <summary>
    /// Implicitly converts a <see cref="RoleBuilder"/> to an <see cref="ApplicationRole"/>.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The constructed <see cref="ApplicationRole"/>.</returns>
    public static implicit operator ApplicationRole(RoleBuilder builder) => builder.Build();
}

/// <summary>
/// Builder for constructing <see cref="RoleDto"/> test fixtures using a fluent API.
/// </summary>
public class RoleDtoBuilder
{
    private Guid _id = Guid.NewGuid();
    private DateTime _createdAt = DateTime.UtcNow;
    private string _createdBy = "TestUser";
    private DateTime? _updatedAt = null;
    private string? _updatedBy = null;
    private string? _name = "TestRole";
    private string? _description = "Test role description";

    /// <summary>
    /// Sets the role DTO's unique identifier.
    /// </summary>
    /// <param name="id">The identifier to assign.</param>
    /// <returns>The current <see cref="RoleDtoBuilder"/> instance for chaining.</returns>
    public RoleDtoBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the role DTO's name.
    /// </summary>
    /// <param name="name">The role name to assign.</param>
    /// <returns>The current <see cref="RoleDtoBuilder"/> instance for chaining.</returns>
    public RoleDtoBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the role DTO's description.
    /// </summary>
    /// <param name="description">The description to assign.</param>
    /// <returns>The current <see cref="RoleDtoBuilder"/> instance for chaining.</returns>
    public RoleDtoBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured <see cref="RoleDto"/> instance.
    /// </summary>
    /// <returns>The constructed <see cref="RoleDto"/>.</returns>
    public RoleDto Build() => new(_id)
    {
        CreatedAt = _createdAt,
        CreatedBy = _createdBy,
        UpdatedAt = _updatedAt,
        UpdatedBy = _updatedBy,
        Name = _name,
        Description = _description
    };

    /// <summary>
    /// Implicitly converts a <see cref="RoleDtoBuilder"/> to a <see cref="RoleDto"/>.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The constructed <see cref="RoleDto"/>.</returns>
    public static implicit operator RoleDto(RoleDtoBuilder builder) => builder.Build();
}

/// <summary>
/// Builder for constructing <see cref="CreateRoleDto"/> test fixtures using a fluent API.
/// </summary>
public class CreateRoleDtoBuilder
{
    private string _name = "NewRole";
    private string? _description = "New role description";

    /// <summary>
    /// Sets the name of the role to create.
    /// </summary>
    /// <param name="name">The role name to assign.</param>
    /// <returns>The current <see cref="CreateRoleDtoBuilder"/> instance for chaining.</returns>
    public CreateRoleDtoBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the description of the role to create.
    /// </summary>
    /// <param name="description">The description to assign.</param>
    /// <returns>The current <see cref="CreateRoleDtoBuilder"/> instance for chaining.</returns>
    public CreateRoleDtoBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured <see cref="CreateRoleDto"/> instance.
    /// </summary>
    /// <returns>The constructed <see cref="CreateRoleDto"/>.</returns>
    public CreateRoleDto Build() => new(_name, _description);

    /// <summary>
    /// Implicitly converts a <see cref="CreateRoleDtoBuilder"/> to a <see cref="CreateRoleDto"/>.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The constructed <see cref="CreateRoleDto"/>.</returns>
    public static implicit operator CreateRoleDto(CreateRoleDtoBuilder builder) => builder.Build();
}

/// <summary>
/// Builder for constructing role-update <see cref="CreateRoleDto"/> test fixtures using a fluent API.
/// </summary>
public class UpdateRoleDtoBuilder
{
    private string _name = "UpdatedRole";
    private string? _description = "Updated role description";

    /// <summary>
    /// Sets the updated role name.
    /// </summary>
    /// <param name="name">The role name to assign.</param>
    /// <returns>The current <see cref="UpdateRoleDtoBuilder"/> instance for chaining.</returns>
    public UpdateRoleDtoBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the updated role description.
    /// </summary>
    /// <param name="description">The description to assign.</param>
    /// <returns>The current <see cref="UpdateRoleDtoBuilder"/> instance for chaining.</returns>
    public UpdateRoleDtoBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured <see cref="CreateRoleDto"/> instance.
    /// </summary>
    /// <returns>The constructed <see cref="CreateRoleDto"/>.</returns>
    public CreateRoleDto Build() => new(_name, _description);

    /// <summary>
    /// Implicitly converts an <see cref="UpdateRoleDtoBuilder"/> to a <see cref="CreateRoleDto"/>.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The constructed <see cref="CreateRoleDto"/>.</returns>
    public static implicit operator CreateRoleDto(UpdateRoleDtoBuilder builder) => builder.Build();
}