using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Application.Services;
using MyApp.Auth.Application.Tests.Builders;
using MyApp.Auth.Application.Tests.Common;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using System.Security.Claims;
using Xunit;

namespace MyApp.Auth.Application.Tests.Services;

public class UserServiceTests : BaseServiceTest
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<RoleManager<ApplicationRole>> _mockRoleManager;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<IPermissionRepository> _mockPermissionRepository;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUserManager = CreateMockUserManager();
        _mockRoleManager = CreateMockRoleManager();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockPermissionRepository = new Mock<IPermissionRepository>();
        _mockLogger = CreateMockLogger<UserService>();
        
        _userService = new UserService(
            _mockUserManager.Object,
            _mockRoleManager.Object,
            _mockUserRepository.Object,
            Mapper,
            _mockLogger.Object,
            _mockHttpContextAccessor.Object,
            _mockPermissionRepository.Object);
    }

    #region GetUserByIdAsync

    [Fact]
    public async Task GetUserByIdAsync_WithValidId_ShouldReturnMappedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        var userDto = new UserDtoBuilder().WithId(userId).Build();

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        MockMapper
            .Setup(x => x.Map<UserDto>(user))
            .Returns(userDto);

        // Act
        var result = await _userService.GetUserByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(userDto);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _userService.GetUserByIdAsync(userId);

        // Assert
        result.Should().BeNull();
        MockMapper.Verify(x => x.Map<UserDto>(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithRepositoryException_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var exception = new Exception("Database error");

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await _userService.GetUserByIdAsync(userId));
    }

    #endregion

    #region GetUserByEmailAsync

    [Fact]
    public async Task GetUserByEmailAsync_WithValidEmail_ShouldReturnMappedUser()
    {
        // Arrange
        var email = "test@example.com";
        var user = new UserBuilder().WithEmail(email).Build();
        var userDto = new UserDtoBuilder().WithEmail(email).Build();

        _mockUserManager
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);

        MockMapper
            .Setup(x => x.Map<UserDto>(user))
            .Returns(userDto);

        // Act
        var result = await _userService.GetUserByEmailAsync(email);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(userDto);
    }

    [Fact]
    public async Task GetUserByEmailAsync_WithInvalidEmail_ShouldReturnNull()
    {
        // Arrange
        var email = "nonexistent@example.com";

        _mockUserManager
            .Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _userService.GetUserByEmailAsync(email);

        // Assert
        result.Should().BeNull();
        MockMapper.Verify(x => x.Map<UserDto>(It.IsAny<ApplicationUser>()), Times.Never);
    }

    #endregion

    #region GetAllUsersAsync

    [Fact]
    public async Task GetAllUsersAsync_ShouldReturnMappedUsers()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new UserBuilder().WithEmail("user1@example.com").Build(),
            new UserBuilder().WithEmail("user2@example.com").Build()
        };

        var userDtos = new List<UserDto>
        {
            new UserDtoBuilder().WithEmail("user1@example.com").Build(),
            new UserDtoBuilder().WithEmail("user2@example.com").Build()
        };

        _mockUserRepository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(users);

        MockMapper
            .Setup(x => x.Map<IEnumerable<UserDto>>(users))
            .Returns(userDtos);

        // Act
        var result = await _userService.GetAllUsersAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(userDtos);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithRepositoryException_ShouldThrowException()
    {
        // Arrange
        var exception = new Exception("Database error");

        _mockUserRepository
            .Setup(x => x.GetAllAsync())
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await _userService.GetAllUsersAsync());
    }

    #endregion

    #region UpdateUserAsync

    [Fact]
    public async Task UpdateUserAsync_WithValidIdAndDto_ShouldUpdateAndReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateUserDto
        {
            FirstName = "UpdatedFirst",
            LastName = "UpdatedLast",
            Email = "updated@example.com"
        };

        var existingUser = new UserBuilder().WithId(userId).Build();

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(existingUser);

        _mockUserManager
            .Setup(x => x.SetEmailAsync(existingUser, updateDto.Email))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager
            .Setup(x => x.SetUserNameAsync(existingUser, updateDto.Email))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager
            .Setup(x => x.UpdateAsync(existingUser))
            .ReturnsAsync(IdentityResult.Success);

        MockMapper
            .Setup(x => x.Map(It.IsAny<UpdateUserDto>(), It.IsAny<ApplicationUser>()))
            .Returns(existingUser);

        // Act
        var result = await _userService.UpdateUserAsync(userId, updateDto);

        // Assert
        result.Should().BeTrue();
        existingUser.FirstName.Should().Be(updateDto.FirstName);
        existingUser.LastName.Should().Be(updateDto.LastName);
        _mockUserManager.Verify(x => x.SetEmailAsync(existingUser, updateDto.Email), Times.Once);
        _mockUserManager.Verify(x => x.SetUserNameAsync(existingUser, updateDto.Email), Times.Once);
        _mockUserManager.Verify(x => x.UpdateAsync(existingUser), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_WithInvalidId_ShouldReturnFalseAndLogWarning()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateUserDto
        {
            FirstName = "UpdatedFirst",
            LastName = "UpdatedLast"
        };

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _userService.UpdateUserAsync(userId, updateDto);

        // Assert
        result.Should().BeFalse();
        _mockUserManager.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        VerifyLoggerCalled(_mockLogger, LogLevel.Warning, Times.Once());
    }

    #endregion

    #region ChangePasswordAsync

    [Fact]
    public async Task ChangePasswordAsync_WithValidCredentials_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentPassword = "CurrentPassword123!";
        var newPassword = "NewPassword123!";

        var user = new UserBuilder().WithId(userId).Build();

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.ChangePasswordAsync(user, currentPassword, newPassword))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _userService.ChangePasswordAsync(userId, currentPassword, newPassword);

        // Assert
        result.Should().BeTrue();
        _mockUserManager.Verify(x => x.ChangePasswordAsync(user, currentPassword, newPassword), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithInvalidCurrentPassword_ShouldReturnFalseAndLogWarning()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var currentPassword = "WrongPassword";
        var newPassword = "NewPassword123!";

        var user = new UserBuilder().WithId(userId).Build();

        var identityErrors = new List<IdentityError>
        {
            new IdentityError { Code = "PasswordMismatch", Description = "Incorrect password" }
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.ChangePasswordAsync(user, currentPassword, newPassword))
            .ReturnsAsync(IdentityResult.Failed(identityErrors.ToArray()));

        // Act
        var result = await _userService.ChangePasswordAsync(userId, currentPassword, newPassword);

        // Assert
        result.Should().BeFalse();
        VerifyLoggerCalled(_mockLogger, LogLevel.Warning, Times.Once());
    }

    #endregion

    #region DeleteUserAsync

    [Fact]
    public async Task DeleteUserAsync_WithValidId_ShouldDeleteAndReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _userService.DeleteUserAsync(userId);

        // Assert
        result.Should().BeTrue();
        _mockUserManager.Verify(x => x.DeleteAsync(user), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_WithInvalidId_ShouldReturnFalseAndLogWarning()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _userService.DeleteUserAsync(userId);

        // Assert
        result.Should().BeFalse();
        _mockUserManager.Verify(x => x.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Never);
        VerifyLoggerCalled(_mockLogger, LogLevel.Warning, Times.Once());
    }

    #endregion

    #region AssignRoleAsync

    [Fact]
    public async Task AssignRoleAsync_WithValidUserAndRole_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleName = "Admin";

        var user = new UserBuilder().WithId(userId).Build();

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockRoleManager
            .Setup(x => x.RoleExistsAsync(roleName))
            .ReturnsAsync(true);

        _mockUserManager
            .Setup(x => x.AddToRoleAsync(user, roleName))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _userService.AssignRoleAsync(userId, roleName);

        // Assert
        result.Should().BeTrue();
        _mockUserManager.Verify(x => x.AddToRoleAsync(user, roleName), Times.Once);
    }

    [Fact]
    public async Task AssignRoleAsync_WithInvalidRole_ShouldReturnFalseAndLogWarning()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleName = "NonExistentRole";

        var user = new UserBuilder().WithId(userId).Build();

        var identityErrors = new List<IdentityError>
        {
            new IdentityError { Code = "InvalidRoleName", Description = "Role does not exist" }
        };

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.AddToRoleAsync(user, roleName))
            .ReturnsAsync(IdentityResult.Failed(identityErrors.ToArray()));

        // Act
        var result = await _userService.AssignRoleAsync(userId, roleName);

        // Assert
        result.Should().BeFalse();
        VerifyLoggerCalled(_mockLogger, LogLevel.Warning, Times.Once());
    }

    #endregion

    #region RemoveRoleAsync

    [Fact]
    public async Task RemoveRoleAsync_WithValidUserAndRole_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleName = "ApplicationUser";

        var user = new UserBuilder().WithId(userId).Build();

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.RemoveFromRoleAsync(user, roleName))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _userService.RemoveRoleAsync(userId, roleName);

        // Assert
        result.Should().BeTrue();
        _mockUserManager.Verify(x => x.RemoveFromRoleAsync(user, roleName), Times.Once);
    }

    #endregion

    #region GetUserRolesAsync

    [Fact]
    public async Task GetUserRolesAsync_WithValidUserId_ShouldReturnUserRoles()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserBuilder().WithId(userId).Build();
        var roles = new List<string> { "Admin", "ApplicationUser" };

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);

        var roleEntities = new List<ApplicationRole>
        {
            new RoleBuilder().WithName("Admin").Build(),
            new RoleBuilder().WithName("ApplicationUser").Build()
        };

        _mockRoleManager
            .Setup(x => x.FindByNameAsync("Admin"))
            .ReturnsAsync(roleEntities[0]);

        _mockRoleManager
            .Setup(x => x.FindByNameAsync("ApplicationUser"))
            .ReturnsAsync(roleEntities[1]);

        MockMapper
            .Setup(x => x.Map<RoleDto>(It.IsAny<ApplicationRole>()))
            .Returns((ApplicationRole r) => new RoleDtoBuilder().WithName(r.Name ?? "").Build());

        // Act
        var result = await _userService.GetUserRolesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserRolesAsync_WithInvalidUserId_ShouldReturnEmptyListAndLogWarning()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _userService.GetUserRolesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockUserManager.Verify(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()), Times.Never);
        VerifyLoggerCalled(_mockLogger, LogLevel.Warning, Times.Once());
    }

    #endregion

    #region GetCurrentUserAsync

    [Fact]
    public async Task GetCurrentUserAsync_WithValidClaimsPrincipal_ShouldReturnMappedUser()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };

        var user = new UserBuilder().WithId(Guid.Parse(userId)).Build();
        var userDto = new UserDtoBuilder().WithId(Guid.Parse(userId)).Build();

        _mockHttpContextAccessor
            .Setup(x => x.HttpContext)
            .Returns(httpContext);

        _mockUserManager
            .Setup(x => x.GetUserAsync(claimsPrincipal))
            .ReturnsAsync(user);

        _mockUserManager
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        _mockRoleManager
            .Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationRole?)null);

        _mockPermissionRepository
            .Setup(x => x.GetAllPermissionsByUserId(user.Id))
            .ReturnsAsync(new List<Permission>());

        MockMapper
            .Setup(x => x.Map<UserDto>(user))
            .Returns(userDto);

        MockMapper
            .Setup(x => x.Map<RoleDto>(It.IsAny<ApplicationRole>()))
            .Returns((ApplicationRole r) => new RoleDtoBuilder().WithName(r.Name ?? "").Build());

        MockMapper
            .Setup(x => x.Map<List<RoleDto?>>(It.IsAny<IEnumerable<RoleDto>>()))
            .Returns((IEnumerable<RoleDto> roles) => roles.Select(r => (RoleDto?)r).ToList());

        MockMapper
            .Setup(x => x.Map<List<PermissionDto?>>(It.IsAny<IEnumerable<Permission>>()))
            .Returns(new List<PermissionDto?>());

        // Act
        var result = await _userService.GetCurrentUserAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(userDto);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithInvalidClaimsPrincipal_ShouldReturnNull()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());

        _mockUserManager
            .Setup(x => x.GetUserAsync(claimsPrincipal))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _userService.GetCurrentUserAsync();

        // Assert
        result.Should().BeNull();
        MockMapper.Verify(x => x.Map<UserDto>(It.IsAny<ApplicationUser>()), Times.Never);
    }

    #endregion

    #region Helper Methods

    private static Mock<UserManager<ApplicationUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static Mock<RoleManager<ApplicationRole>> CreateMockRoleManager()
    {
        var store = new Mock<IRoleStore<ApplicationRole>>();
        return new Mock<RoleManager<ApplicationRole>>(store.Object, null!, null!, null!, null!);
    }

    #endregion
}