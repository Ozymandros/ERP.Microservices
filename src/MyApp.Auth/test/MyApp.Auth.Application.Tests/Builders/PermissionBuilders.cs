using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Domain.Entities;

namespace MyApp.Auth.Application.Tests.Builders;

public class PermissionBuilder
{
    private Permission _permission = new(Guid.NewGuid())
    {
        Module = "TestModule",
        Action = "TestAction",
        Description = "Test permission description"
    };

    public PermissionBuilder WithId(Guid id)
    {
        _permission.Id = id;
        return this;
    }

    public PermissionBuilder WithModule(string module)
    {
        _permission.Module = module;
        return this;
    }

    public PermissionBuilder WithAction(string action)
    {
        _permission.Action = action;
        return this;
    }

    public PermissionBuilder WithDescription(string? description)
    {
        _permission.Description = description;
        return this;
    }

    public Permission Build() => _permission;

    public static implicit operator Permission(PermissionBuilder builder) => builder.Build();
}

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

    public PermissionDtoBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public PermissionDtoBuilder WithModule(string module)
    {
        _module = module;
        return this;
    }

    public PermissionDtoBuilder WithAction(string action)
    {
        _action = action;
        return this;
    }

    public PermissionDtoBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

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

    public static implicit operator PermissionDto(PermissionDtoBuilder builder) => builder.Build();
}

public class CreatePermissionDtoBuilder
{
    private string _module = "TestModule";
    private string _action = "TestAction";
    private string? _description = "Test permission description";

    public CreatePermissionDtoBuilder WithModule(string module)
    {
        _module = module;
        return this;
    }

    public CreatePermissionDtoBuilder WithAction(string action)
    {
        _action = action;
        return this;
    }

    public CreatePermissionDtoBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public CreatePermissionDto Build() => new(_module, _action, _description);

    public static implicit operator CreatePermissionDto(CreatePermissionDtoBuilder builder) => builder.Build();
}