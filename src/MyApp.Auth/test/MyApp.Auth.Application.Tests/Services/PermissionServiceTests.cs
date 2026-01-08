using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Application.Services;
using MyApp.Auth.Application.Tests.Builders;
using MyApp.Auth.Application.Tests.Common;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using Xunit;

namespace MyApp.Auth.Application.Tests.Services;

public class PermissionServiceTests : BaseServiceTest
{
    private readonly Mock<IPermissionRepository> _mockPermissionRepository;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<ILogger<PermissionService>> _mockLogger;
    private readonly PermissionService _permissionService;

    public PermissionServiceTests()
    {
        _mockPermissionRepository = new Mock<IPermissionRepository>();
        _mockUserManager = CreateMockUserManager();
        _mockLogger = CreateMockLogger<PermissionService>();
        _permissionService = new PermissionService(
            _mockUserManager.Object,
            _mockPermissionRepository.Object, 
            Mapper, 
            _mockLogger.Object);
    }

    #region HasPermissionAsync(Guid userId, string module, string action)

    [Fact]
    public async Task HasPermissionAsync_WithValidUserIdModuleAndAction_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var module = "Orders";
        var action = "Create";
        var permissions = new List<Permission>
        {
            new PermissionBuilder().WithModule(module).WithAction(action).Build()
        };

        _mockPermissionRepository
            .Setup(x => x.GetAllPermissionsByUserId(userId))
            .ReturnsAsync(permissions);

        // Act
        var result = await _permissionService.HasPermissionAsync(userId, module, action);

        // Assert
        result.Should().BeTrue();
        _mockPermissionRepository.Verify(x => x.GetAllPermissionsByUserId(userId), Times.Once);
    }

    [Fact]
    public async Task HasPermissionAsync_WithInvalidUserIdModuleAndAction_ShouldReturnFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var module = "Orders";
        var action = "Delete";
        var permissions = new List<Permission>
        {
            new PermissionBuilder().WithModule(module).WithAction("Create").Build()
        };

        _mockPermissionRepository
            .Setup(x => x.GetAllPermissionsByUserId(userId))
            .ReturnsAsync(permissions);

        // Act
        var result = await _permissionService.HasPermissionAsync(userId, module, action);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_WithRepositoryException_ShouldReturnFalseAndLogError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var module = "Orders";
        var action = "Create";
        var exception = new Exception("Database error");

        _mockPermissionRepository
            .Setup(x => x.GetAllPermissionsByUserId(userId))
            .ThrowsAsync(exception);

        // Act
        var result = await _permissionService.HasPermissionAsync(userId, module, action);

        // Assert
        result.Should().BeFalse();
        VerifyLoggerCalled(_mockLogger, LogLevel.Error, Times.Once());
    }

    #endregion

    #region HasPermissionAsync(string? username, string module, string action)

    [Fact]
    public async Task HasPermissionAsync_WithValidUsernameModuleAndAction_ShouldReturnTrue()
    {
        // Arrange
        var username = "testuser";
        var module = "Orders";
        var action = "Create";
        var user = new UserBuilder().WithUserName(username).Build();
        var permissions = new List<Permission>
        {
            new PermissionBuilder().WithModule(module).WithAction(action).Build()
        };

        _mockUserManager
            .Setup(x => x.FindByNameAsync(username))
            .ReturnsAsync(user);

        _mockPermissionRepository
            .Setup(x => x.GetByUserName(username, module, action))
            .ReturnsAsync(permissions);

        // Act
        var result = await _permissionService.HasPermissionAsync(username, module, action);

        // Assert
        result.Should().BeTrue();
        _mockPermissionRepository.Verify(x => x.GetByUserName(username, module, action), Times.Once);
    }

    [Fact]
    public async Task HasPermissionAsync_WithNullUsername_ShouldReturnFalse()
    {
        // Arrange
        string? username = null;
        var module = "Orders";
        var action = "Create";

        // Act
        var result = await _permissionService.HasPermissionAsync(username, module, action);

        // Assert
        result.Should().BeFalse();
        _mockPermissionRepository.Verify(x => x.GetByUserName(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HasPermissionAsync_WithEmptyUsername_ShouldReturnFalse()
    {
        // Arrange
        var username = "";
        var module = "Orders";
        var action = "Create";

        // Act
        var result = await _permissionService.HasPermissionAsync(username, module, action);

        // Assert
        result.Should().BeFalse();
        _mockPermissionRepository.Verify(x => x.GetByUserName(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HasPermissionAsync_WithUsernameButNoUserPermissions_ShouldCheckRolePermissions()
    {
        // Arrange
        var username = "testuser";
        var module = "Orders";
        var action = "Create";
        var user = new UserBuilder().WithUserName(username).Build();
        var rolePermissions = new List<Permission>
        {
            new PermissionBuilder().WithModule(module).WithAction(action).Build()
        };

        _mockUserManager
            .Setup(x => x.FindByNameAsync(username))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        _mockPermissionRepository
            .Setup(x => x.GetByUserName(username, module, action))
            .ReturnsAsync(new List<Permission>());

        _mockPermissionRepository
            .Setup(x => x.GetByRoleName("User", module, action))
            .ReturnsAsync(rolePermissions);

        // Act
        var result = await _permissionService.HasPermissionAsync(username, module, action);

        // Assert
        result.Should().BeTrue();
        _mockPermissionRepository.Verify(x => x.GetByUserName(username, module, action), Times.Once);
        _mockPermissionRepository.Verify(x => x.GetByRoleName("User", module, action), Times.Once);
    }

    #endregion

    #region GetAllPermissionsAsync

    [Fact]
    public async Task GetAllPermissionsAsync_ShouldReturnMappedPermissions()
    {
        // Arrange
        var permissions = new List<Permission>
        {
            new PermissionBuilder().WithModule("Orders").WithAction("Create").Build(),
            new PermissionBuilder().WithModule("Orders").WithAction("Read").Build()
        };

        var permissionDtos = new List<PermissionDto>
        {
            new PermissionDtoBuilder().WithModule("Orders").WithAction("Create").Build(),
            new PermissionDtoBuilder().WithModule("Orders").WithAction("Read").Build()
        };

        _mockPermissionRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(permissions);

        MockMapper
            .Setup(x => x.Map<IEnumerable<PermissionDto>>(permissions))
            .Returns(permissionDtos);

        // Act
        var result = await _permissionService.GetAllPermissionsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(permissionDtos);
    }

    #endregion

    #region GetPermissionByIdAsync

    [Fact]
    public async Task GetPermissionByIdAsync_WithValidId_ShouldReturnMappedPermission()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var permission = new PermissionBuilder().WithId(permissionId).Build();
        var permissionDto = new PermissionDtoBuilder().WithId(permissionId).Build();

        _mockPermissionRepository
            .Setup(x => x.GetByIdAsync(permissionId))
            .ReturnsAsync(permission);

        MockMapper
            .Setup(x => x.Map<PermissionDto>(permission))
            .Returns(permissionDto);

        // Act
        var result = await _permissionService.GetPermissionByIdAsync(permissionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(permissionDto);
    }

    [Fact]
    public async Task GetPermissionByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var permissionId = Guid.NewGuid();

        _mockPermissionRepository
            .Setup(x => x.GetByIdAsync(permissionId))
            .ReturnsAsync((Permission?)null);

        // Act
        var result = await _permissionService.GetPermissionByIdAsync(permissionId);

        // Assert
        result.Should().BeNull();
        MockMapper.Verify(x => x.Map<PermissionDto>(It.IsAny<Permission>()), Times.Never);
    }

    #endregion

    #region GetPermissionByModuleActionAsync

    [Fact]
    public async Task GetPermissionByModuleActionAsync_WithValidModuleAndAction_ShouldReturnMappedPermission()
    {
        // Arrange
        var module = "Orders";
        var action = "Create";
        var permission = new PermissionBuilder().WithModule(module).WithAction(action).Build();
        var permissionDto = new PermissionDtoBuilder().WithModule(module).WithAction(action).Build();

        _mockPermissionRepository
            .Setup(x => x.GetByUserName("", module, action))
            .ReturnsAsync(new List<Permission> { permission });

        MockMapper
            .Setup(x => x.Map<PermissionDto>(permission))
            .Returns(permissionDto);

        // Act
        var result = await _permissionService.GetPermissionByModuleActionAsync(module, action);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(permissionDto);
    }

    [Fact]
    public async Task GetPermissionByModuleActionAsync_WithInvalidModuleAndAction_ShouldReturnNull()
    {
        // Arrange
        var module = "NonExistent";
        var action = "NonExistent";

        _mockPermissionRepository
            .Setup(x => x.GetByUserName("", module, action))
            .ReturnsAsync(new List<Permission>());

        // Act
        var result = await _permissionService.GetPermissionByModuleActionAsync(module, action);

        // Assert
        result.Should().BeNull();
        MockMapper.Verify(x => x.Map<PermissionDto>(It.IsAny<Permission>()), Times.Never);
    }

    [Fact]
    public async Task GetPermissionByModuleActionAsync_WithRepositoryException_ShouldThrowException()
    {
        // Arrange
        var module = "Orders";
        var action = "Create";
        var exception = new Exception("Database error");

        _mockPermissionRepository
            .Setup(x => x.GetByUserName("", module, action))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await _permissionService.GetPermissionByModuleActionAsync(module, action));
    }

    #endregion

    #region CreatePermissionAsync

    [Fact]
    public async Task CreatePermissionAsync_WithValidDto_ShouldCreateAndReturnMappedPermission()
    {
        // Arrange
        var createDto = new CreatePermissionDtoBuilder()
            .WithModule("Orders")
            .WithAction("Create")
            .Build();

        var existingPermissions = new List<Permission>();
        var expectedPermissionDto = new PermissionDtoBuilder()
            .WithModule("Orders")
            .WithAction("Create")
            .Build();

        _mockPermissionRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(existingPermissions);

        _mockPermissionRepository
            .Setup(x => x.AddAsync(It.IsAny<Permission>()))
            .ReturnsAsync((Permission p) => p);

        MockMapper
            .Setup(x => x.Map<PermissionDto>(It.IsAny<Permission>()))
            .Returns(expectedPermissionDto);

        // Act
        var result = await _permissionService.CreatePermissionAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedPermissionDto);
        _mockPermissionRepository.Verify(x => x.AddAsync(It.Is<Permission>(p => 
            p.Module == createDto.Module && 
            p.Action == createDto.Action && 
            p.Description == createDto.Description)), Times.Once);
    }

    [Fact]
    public async Task CreatePermissionAsync_WithDuplicateModuleAndAction_ShouldReturnNullAndLogWarning()
    {
        // Arrange
        var createDto = new CreatePermissionDtoBuilder()
            .WithModule("Orders")
            .WithAction("Create")
            .Build();

        var existingPermissions = new List<Permission>
        {
            new PermissionBuilder().WithModule("Orders").WithAction("Create").Build()
        };

        _mockPermissionRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(existingPermissions);

        // Act
        var result = await _permissionService.CreatePermissionAsync(createDto);

        // Assert
        result.Should().BeNull();
        _mockPermissionRepository.Verify(x => x.AddAsync(It.IsAny<Permission>()), Times.Never);
        VerifyLoggerCalled(_mockLogger, LogLevel.Warning, Times.Once());
    }

    [Fact]
    public async Task CreatePermissionAsync_WithRepositoryException_ShouldReturnNullAndLogError()
    {
        // Arrange
        var createDto = new CreatePermissionDtoBuilder().Build();
        var exception = new Exception("Database error");

        _mockPermissionRepository
            .Setup(x => x.GetAllAsync())
            .ThrowsAsync(exception);

        // Act
        var result = await _permissionService.CreatePermissionAsync(createDto);

        // Assert
        result.Should().BeNull();
        VerifyLoggerCalled(_mockLogger, LogLevel.Error, Times.Once());
    }

    #endregion

    #region UpdatePermissionAsync

    [Fact]
    public async Task UpdatePermissionAsync_WithValidIdAndDto_ShouldUpdateAndReturnTrue()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var updateDto = new UpdatePermissionDto("UpdatedModule", "UpdatedAction", "Updated description");

        var existingPermission = new PermissionBuilder().WithId(permissionId).Build();

        _mockPermissionRepository
            .Setup(x => x.GetByIdAsync(permissionId))
            .ReturnsAsync(existingPermission);

        _mockPermissionRepository
            .Setup(x => x.UpdateAsync(existingPermission))
            .ReturnsAsync(existingPermission);

        // Act
        var result = await _permissionService.UpdatePermissionAsync(permissionId, updateDto);

        // Assert
        result.Should().BeTrue();
        existingPermission.Module.Should().Be(updateDto.Module);
        existingPermission.Action.Should().Be(updateDto.Action);
        existingPermission.Description.Should().Be(updateDto.Description);
        _mockPermissionRepository.Verify(x => x.UpdateAsync(existingPermission), Times.Once);
    }

    [Fact]
    public async Task UpdatePermissionAsync_WithInvalidId_ShouldReturnFalseAndLogWarning()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var updateDto = new UpdatePermissionDto("UpdatedModule", "UpdatedAction");

        _mockPermissionRepository
            .Setup(x => x.GetByIdAsync(permissionId))
            .ReturnsAsync((Permission?)null);

        // Act
        var result = await _permissionService.UpdatePermissionAsync(permissionId, updateDto);

        // Assert
        result.Should().BeFalse();
        _mockPermissionRepository.Verify(x => x.UpdateAsync(It.IsAny<Permission>()), Times.Never);
        VerifyLoggerCalled(_mockLogger, LogLevel.Warning, Times.Once());
    }

    [Fact]
    public async Task UpdatePermissionAsync_WithRepositoryException_ShouldReturnFalseAndLogError()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var updateDto = new UpdatePermissionDto("UpdatedModule", "UpdatedAction");

        var exception = new Exception("Database error");

        _mockPermissionRepository
            .Setup(x => x.GetByIdAsync(permissionId))
            .ThrowsAsync(exception);

        // Act
        var result = await _permissionService.UpdatePermissionAsync(permissionId, updateDto);

        // Assert
        result.Should().BeFalse();
        VerifyLoggerCalled(_mockLogger, LogLevel.Error, Times.Once());
    }

    #endregion

    #region DeletePermissionAsync

    [Fact]
    public async Task DeletePermissionAsync_WithValidId_ShouldDeleteAndReturnTrue()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var permission = new PermissionBuilder().WithId(permissionId).Build();

        _mockPermissionRepository
            .Setup(x => x.GetByIdAsync(permissionId))
            .ReturnsAsync(permission);

        _mockPermissionRepository
            .Setup(x => x.DeleteAsync(permission))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _permissionService.DeletePermissionAsync(permissionId);

        // Assert
        result.Should().BeTrue();
        _mockPermissionRepository.Verify(x => x.DeleteAsync(permission), Times.Once);
    }

    [Fact]
    public async Task DeletePermissionAsync_WithInvalidId_ShouldReturnFalseAndLogWarning()
    {
        // Arrange
        var permissionId = Guid.NewGuid();

        _mockPermissionRepository
            .Setup(x => x.GetByIdAsync(permissionId))
            .ReturnsAsync((Permission?)null);

        // Act
        var result = await _permissionService.DeletePermissionAsync(permissionId);

        // Assert
        result.Should().BeFalse();
        _mockPermissionRepository.Verify(x => x.DeleteAsync(It.IsAny<Permission>()), Times.Never);
        VerifyLoggerCalled(_mockLogger, LogLevel.Warning, Times.Once());
    }

    [Fact]
    public async Task DeletePermissionAsync_WithRepositoryException_ShouldReturnFalseAndLogError()
    {
        // Arrange
        var permissionId = Guid.NewGuid();
        var exception = new Exception("Database error");

        _mockPermissionRepository
            .Setup(x => x.GetByIdAsync(permissionId))
            .ThrowsAsync(exception);

        // Act
        var result = await _permissionService.DeletePermissionAsync(permissionId);

        // Assert
        result.Should().BeFalse();
        VerifyLoggerCalled(_mockLogger, LogLevel.Error, Times.Once());
    }

    #endregion

    #region Helper Methods

    private static Mock<UserManager<ApplicationUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    #endregion
}