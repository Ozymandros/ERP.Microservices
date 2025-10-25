using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Domain.Entities;

namespace MyApp.Auth.Application.Tests.Builders;

public class PermissionBuilder
{
    private Permission _permission = new()
    {
        Id = Guid.NewGuid(),
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
    private PermissionDto _permissionDto = new()
    {
        Id = Guid.NewGuid(),
        Module = "TestModule",
        Action = "TestAction",
        Description = "Test permission description"
    };

    public PermissionDtoBuilder WithId(Guid id)
    {
        _permissionDto.Id = id;
        return this;
    }

    public PermissionDtoBuilder WithModule(string module)
    {
        _permissionDto.Module = module;
        return this;
    }

    public PermissionDtoBuilder WithAction(string action)
    {
        _permissionDto.Action = action;
        return this;
    }

    public PermissionDtoBuilder WithDescription(string? description)
    {
        _permissionDto.Description = description;
        return this;
    }

    public PermissionDto Build() => _permissionDto;

    public static implicit operator PermissionDto(PermissionDtoBuilder builder) => builder.Build();
}

public class CreatePermissionDtoBuilder
{
    private CreatePermissionDto _createPermissionDto = new()
    {
        Module = "TestModule",
        Action = "TestAction",
        Description = "Test permission description"
    };

    public CreatePermissionDtoBuilder WithModule(string module)
    {
        _createPermissionDto.Module = module;
        return this;
    }

    public CreatePermissionDtoBuilder WithAction(string action)
    {
        _createPermissionDto.Action = action;
        return this;
    }

    public CreatePermissionDtoBuilder WithDescription(string? description)
    {
        _createPermissionDto.Description = description;
        return this;
    }

    public CreatePermissionDto Build() => _createPermissionDto;

    public static implicit operator CreatePermissionDto(CreatePermissionDtoBuilder builder) => builder.Build();
}