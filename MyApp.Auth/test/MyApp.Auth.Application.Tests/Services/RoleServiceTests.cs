using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Auth.Application.Contracts;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Application.Services;
using MyApp.Auth.Application.Tests.Builders;
using MyApp.Auth.Application.Tests.Common;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using Xunit;

namespace MyApp.Auth.Application.Tests.Services;

public class RoleServiceTests : BaseServiceTest
{
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<RoleService>> _mockLogger;
    private readonly RoleService _roleService;

    public RoleServiceTests()
    {
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockRoleManager = CreateMockRoleManager();
        _mockUserManager = CreateMockUserManager();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = CreateMockLogger<RoleService>();
        
        _roleService = new RoleService(
            _mockRoleManager.Object,
            _mockUserManager.Object,
            _mockRoleRepository.Object,
            _mockUserRepository.Object,
            Mapper,
            _mockLogger.Object);
    }

    #region GetAllRolesAsync

    [Fact]
    public async Task GetAllRolesAsync_ShouldReturnMappedRoles()
    {
        // Arrange
        var roles = new List<ApplicationRole>
        {
            new RoleBuilder().WithName("Admin").Build(),
            new RoleBuilder().WithName("User").Build()
        };

        var roleDtos = new List<RoleDto>
        {
            new RoleDtoBuilder().WithName("Admin").Build(),
            new RoleDtoBuilder().WithName("User").Build()
        };

        _mockRoleRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(roles);

        MockMapper
            .Setup(x => x.Map<IEnumerable<RoleDto>>(roles))
            .Returns(roleDtos);

        // Act
        var result = await _roleService.GetAllRolesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(roleDtos);
    }

    [Fact]
    public async Task GetAllRolesAsync_WithRepositoryException_ShouldReturnEmptyListAndLogError()
    {
        // Arrange
        var exception = new Exception("Database error");

        _mockRoleRepository
            .Setup(x => x.GetAllAsync())
            .ThrowsAsync(exception);

        // Act
        var result = await _roleService.GetAllRolesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        VerifyLoggerCalled(_mockLogger, LogLevel.Error, Times.Once());
    }

    #endregion

    #region GetRoleByIdAsync

    [Fact]
    public async Task GetRoleByIdAsync_WithValidId_ShouldReturnMappedRole()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var role = new RoleBuilder().WithId(roleId).WithName("Admin").Build();
        var roleDto = new RoleDtoBuilder().WithId(roleId).WithName("Admin").Build();

        _mockRoleRepository
            .Setup(x => x.GetByIdAsync(roleId))
            .ReturnsAsync(role);

        MockMapper
            .Setup(x => x.Map<RoleDto>(role))
            .Returns(roleDto);

        // Act
        var result = await _roleService.GetRoleByIdAsync(roleId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(roleDto);
    }

    [Fact]
    public async Task GetRoleByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var roleId = Guid.NewGuid();

        _mockRoleRepository
            .Setup(x => x.GetByIdAsync(roleId))
            .ReturnsAsync((ApplicationRole?)null);

        // Act
        var result = await _roleService.GetRoleByIdAsync(roleId);

        // Assert
        result.Should().BeNull();
        MockMapper.Verify(x => x.Map<RoleDto>(It.IsAny<ApplicationRole>()), Times.Never);
    }

    #endregion

    #region CreateRoleAsync

    [Fact]
    public async Task CreateRoleAsync_WithValidDto_ShouldCreateAndReturnMappedRole()
    {
        // Arrange
        var createDto = new CreateRoleDto
        {
            Name = "NewRole",
            Description = "New role description"
        };

        var createdRole = new RoleBuilder()
            .WithName(createDto.Name)
            .WithDescription(createDto.Description)
            .Build();

        var roleDto = new RoleDtoBuilder()
            .WithName(createDto.Name)
            .WithDescription(createDto.Description)
            .Build();

        _mockRoleManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationRole>()))
            .ReturnsAsync(IdentityResult.Success);

        MockMapper
            .Setup(x => x.Map<RoleDto>(It.IsAny<ApplicationRole>()))
            .Returns(roleDto);

        // Act
        var result = await _roleService.CreateRoleAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(roleDto);
        _mockRoleManager.Verify(x => x.CreateAsync(It.Is<ApplicationRole>(r => 
            r.Name == createDto.Name && 
            r.Description == createDto.Description)), Times.Once);
    }

    [Fact]
    public async Task CreateRoleAsync_WithDuplicateName_ShouldReturnNullAndLogWarning()
    {
        // Arrange
        var createDto = new CreateRoleDto
        {
            Name = "ExistingRole",
            Description = "Description"
        };

        var identityErrors = new List<IdentityError>
        {
            new IdentityError { Code = "DuplicateRoleName", Description = "Role name already exists" }
        };

        _mockRoleManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationRole>()))
            .ReturnsAsync(IdentityResult.Failed(identityErrors.ToArray()));

        // Act
        var result = await _roleService.CreateRoleAsync(createDto);

        // Assert
        result.Should().BeNull();
        VerifyLoggerCalled(_mockLogger, LogLevel.Warning, Times.Once());
    }

    #endregion

    #region UpdateRoleAsync

    [Fact]
    public async Task UpdateRoleAsync_WithValidIdAndDto_ShouldUpdateAndReturnTrue()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var updateDto = new CreateRoleDto
        {
            Name = "UpdatedRole",
            Description = "Updated description"
        };

        var existingRole = new RoleBuilder().WithId(roleId).Build();

        _mockRoleRepository
            .Setup(x => x.GetByIdAsync(roleId))
            .ReturnsAsync(existingRole);

        _mockRoleManager
            .Setup(x => x.UpdateAsync(existingRole))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _roleService.UpdateRoleAsync(roleId, updateDto);

        // Assert
        result.Should().BeTrue();
        existingRole.Name.Should().Be(updateDto.Name);
        existingRole.Description.Should().Be(updateDto.Description);
        _mockRoleManager.Verify(x => x.UpdateAsync(existingRole), Times.Once);
    }

    [Fact]
    public async Task UpdateRoleAsync_WithInvalidId_ShouldReturnFalseAndLogWarning()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var updateDto = new CreateRoleDto
        {
            Name = "UpdatedRole",
            Description = "Updated description"
        };

        _mockRoleRepository
            .Setup(x => x.GetByIdAsync(roleId))
            .ReturnsAsync((ApplicationRole?)null);

        // Act
        var result = await _roleService.UpdateRoleAsync(roleId, updateDto);

        // Assert
        result.Should().BeFalse();
        _mockRoleManager.Verify(x => x.UpdateAsync(It.IsAny<ApplicationRole>()), Times.Never);
        VerifyLoggerCalled(_mockLogger, LogLevel.Warning, Times.Once());
    }

    #endregion

    #region DeleteRoleAsync

    [Fact]
    public async Task DeleteRoleAsync_WithValidId_ShouldDeleteAndReturnTrue()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var role = new RoleBuilder().WithId(roleId).Build();

        _mockRoleRepository
            .Setup(x => x.GetByIdAsync(roleId))
            .ReturnsAsync(role);

        _mockRoleManager
            .Setup(x => x.DeleteAsync(role))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _roleService.DeleteRoleAsync(roleId);

        // Assert
        result.Should().BeTrue();
        _mockRoleManager.Verify(x => x.DeleteAsync(role), Times.Once);
    }

    [Fact]
    public async Task DeleteRoleAsync_WithInvalidId_ShouldReturnFalseAndLogWarning()
    {
        // Arrange
        var roleId = Guid.NewGuid();

        _mockRoleRepository
            .Setup(x => x.GetByIdAsync(roleId))
            .ReturnsAsync((ApplicationRole?)null);

        // Act
        var result = await _roleService.DeleteRoleAsync(roleId);

        // Assert
        result.Should().BeFalse();
        _mockRoleManager.Verify(x => x.DeleteAsync(It.IsAny<ApplicationRole>()), Times.Never);
        VerifyLoggerCalled(_mockLogger, LogLevel.Warning, Times.Once());
    }

    #endregion

    #region AddPermissionToRole

    [Fact]
    public async Task AddPermissionToRole_WithValidIds_ShouldAddPermissionAndReturnTrue()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var createDto = new CreateRolePermissionDto(roleId, permissionId);

        // Act
        var result = await _roleService.AddPermissionToRole(createDto);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region RemovePermissionFromRoleAsync

    [Fact]
    public async Task RemovePermissionFromRoleAsync_WithValidIds_ShouldRemovePermissionAndReturnTrue()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var deleteDto = new DeleteRolePermissionDto(roleId, permissionId);

        // Act
        var result = await _roleService.RemovePermissionFromRoleAsync(deleteDto);

        // Assert
        result.Should().BeTrue();
    }

    #endregion
    
    #region HasPermissionAsync

    [Fact]
    public async Task HasPermissionAsync_WithValidRoleAndPermission_ShouldReturnTrue()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        // Act  
        var result = await _roleService.HasPermissionAsync(roleId, permissionId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_WithInvalidRoleOrPermission_ShouldReturnFalse()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();

        // Act
        var result = await _roleService.HasPermissionAsync(roleId, permissionId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

#region GetPermissionsForRoleAsync

[Fact]
public async Task GetPermissionsForRoleAsync_WithValidRoleId_ShouldReturnMappedPermissions()
{
    // Arrange
    var roleId = Guid.NewGuid();
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

    _mockRoleRepository
        .Setup(x => x.GetPermissionsForRoleAsync(roleId))
        .ReturnsAsync(permissions);

    MockMapper
        .Setup(x => x.Map<IEnumerable<PermissionDto>>(permissions))
        .Returns(permissionDtos);

    // Act
    var result = await _roleService.GetPermissionsForRoleAsync(roleId);

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(2);
    result.Should().BeEquivalentTo(permissionDtos);
}

[Fact]
public async Task GetPermissionsForRoleAsync_WithRepositoryException_ShouldReturnEmptyListAndLogError()
{
    // Arrange
    var roleId = Guid.NewGuid();
    var exception = new Exception("Database error");

    _mockRoleRepository
        .Setup(x => x.GetPermissionsForRoleAsync(roleId))
        .ThrowsAsync(exception);

    // Act
    var result = await _roleService.GetPermissionsForRoleAsync(roleId);

    // Assert
    result.Should().NotBeNull();
    result.Should().BeEmpty();
    VerifyLoggerCalled(_mockLogger, LogLevel.Error, Times.Once());
}

#endregion

#region Helper Methods

private static Mock<RoleManager<ApplicationRole>> CreateMockRoleManager()
{
    var store = new Mock<IRoleStore<ApplicationRole>>();
    return new Mock<RoleManager<ApplicationRole>>(store.Object, null!, null!, null!, null!);
}

private static Mock<UserManager<ApplicationUser>> CreateMockUserManager()
{
    var store = new Mock<IUserStore<ApplicationUser>>();
    return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
}

#endregion
}