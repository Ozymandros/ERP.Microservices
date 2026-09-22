using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Domain.Entities;

namespace MyApp.Auth.Application.Tests.Builders;

/// <summary>
/// Builder for constructing <see cref="Permission"/> domain entity test fixtures using a fluent API.
/// </summary>
public class PermissionBuilder
{
    private Permission _permission = new(Guid.NewGuid())
    {
        Module = "TestModule",
        Action = "TestAction",
        Description = "Test permission description"
    };

    /// <summary>
    /// Sets the permission's unique identifier.
    /// </summary>
    /// <param name="id">The identifier to assign.</param>
    /// <returns>The current <see cref="PermissionBuilder"/> instance for chaining.</returns>
    public PermissionBuilder WithId(Guid id)
    {
        _permission.Id = id;
        return this;
    }

    /// <summary>
    /// Sets the module this permission applies to.
    /// </summary>
    /// <param name="module">The module name to assign.</param>
    /// <returns>The current <see cref="PermissionBuilder"/> instance for chaining.</returns>
    public PermissionBuilder WithModule(string module)
    {
        _permission.Module = module;
        return this;
    }

    /// <summary>
    /// Sets the action this permission grants.
    /// </summary>
    /// <param name="action">The action name to assign.</param>
    /// <returns>The current <see cref="PermissionBuilder"/> instance for chaining.</returns>
    public PermissionBuilder WithAction(string action)
    {
        _permission.Action = action;
        return this;
    }

    /// <summary>
    /// Sets the optional description for the permission.
    /// </summary>
    /// <param name="description">The description to assign, or <c>null</c> to clear it.</param>
    /// <returns>The current <see cref="PermissionBuilder"/> instance for chaining.</returns>
    public PermissionBuilder WithDescription(string? description)
    {
        _permission.Description = description;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured <see cref="Permission"/> instance.
    /// </summary>
    /// <returns>The constructed <see cref="Permission"/>.</returns>
    public Permission Build() => _permission;

    /// <summary>
    /// Implicitly converts a <see cref="PermissionBuilder"/> to a <see cref="Permission"/>.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The constructed <see cref="Permission"/>.</returns>
    public static implicit operator Permission(PermissionBuilder builder) => builder.Build();
}

/// <summary>
/// Builder for constructing <see cref="PermissionDto"/> test fixtures using a fluent API.
/// </summary>
public class PermissionDtoBuilder
{
    private Guid _id = Guid.NewGuid();
    private DateTime _createdAt = DateTime.UtcNow;
    private string _createdBy = "TestUser";
    private DateTime? _updatedAt = null;
    private string? _updatedBy = null;
    private string _module = "TestModule";
    private string _action = "TestAction";
    private string? _description = "Test permission description";

    /// <summary>
    /// Sets the permission DTO's unique identifier.
    /// </summary>
    /// <param name="id">The identifier to assign.</param>
    /// <returns>The current <see cref="PermissionDtoBuilder"/> instance for chaining.</returns>
    public PermissionDtoBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the module this permission applies to.
    /// </summary>
    /// <param name="module">The module name to assign.</param>
    /// <returns>The current <see cref="PermissionDtoBuilder"/> instance for chaining.</returns>
    public PermissionDtoBuilder WithModule(string module)
    {
        _module = module;
        return this;
    }

    /// <summary>
    /// Sets the action this permission grants.
    /// </summary>
    /// <param name="action">The action name to assign.</param>
    /// <returns>The current <see cref="PermissionDtoBuilder"/> instance for chaining.</returns>
    public PermissionDtoBuilder WithAction(string action)
    {
        _action = action;
        return this;
    }

    /// <summary>
    /// Sets the optional description for the permission DTO.
    /// </summary>
    /// <param name="description">The description to assign, or <c>null</c> to clear it.</param>
    /// <returns>The current <see cref="PermissionDtoBuilder"/> instance for chaining.</returns>
    public PermissionDtoBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured <see cref="PermissionDto"/> instance.
    /// </summary>
    /// <returns>The constructed <see cref="PermissionDto"/>.</returns>
    public PermissionDto Build() => new(_id)
    {
        CreatedAt = _createdAt,
        CreatedBy = _createdBy,
        UpdatedAt = _updatedAt,
        UpdatedBy = _updatedBy,
        Module = _module,
        Action = _action,
        Description = _description
    };

    /// <summary>
    /// Implicitly converts a <see cref="PermissionDtoBuilder"/> to a <see cref="PermissionDto"/>.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The constructed <see cref="PermissionDto"/>.</returns>
    public static implicit operator PermissionDto(PermissionDtoBuilder builder) => builder.Build();
}

/// <summary>
/// Builder for constructing <see cref="CreatePermissionDto"/> test fixtures using a fluent API.
/// </summary>
public class CreatePermissionDtoBuilder
{
    private string _module = "TestModule";
    private string _action = "TestAction";
    private string? _description = "Test permission description";

    /// <summary>
    /// Sets the module for the new permission.
    /// </summary>
    /// <param name="module">The module name to assign.</param>
    /// <returns>The current <see cref="CreatePermissionDtoBuilder"/> instance for chaining.</returns>
    public CreatePermissionDtoBuilder WithModule(string module)
    {
        _module = module;
        return this;
    }

    /// <summary>
    /// Sets the action for the new permission.
    /// </summary>
    /// <param name="action">The action name to assign.</param>
    /// <returns>The current <see cref="CreatePermissionDtoBuilder"/> instance for chaining.</returns>
    public CreatePermissionDtoBuilder WithAction(string action)
    {
        _action = action;
        return this;
    }

    /// <summary>
    /// Sets the optional description for the new permission.
    /// </summary>
    /// <param name="description">The description to assign, or <c>null</c> to omit it.</param>
    /// <returns>The current <see cref="CreatePermissionDtoBuilder"/> instance for chaining.</returns>
    public CreatePermissionDtoBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured <see cref="CreatePermissionDto"/> instance.
    /// </summary>
    /// <returns>The constructed <see cref="CreatePermissionDto"/>.</returns>
    public CreatePermissionDto Build() => new(_module, _action, _description);

    /// <summary>
    /// Implicitly converts a <see cref="CreatePermissionDtoBuilder"/> to a <see cref="CreatePermissionDto"/>.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The constructed <see cref="CreatePermissionDto"/>.</returns>
    public static implicit operator CreatePermissionDto(CreatePermissionDtoBuilder builder) => builder.Build();
}