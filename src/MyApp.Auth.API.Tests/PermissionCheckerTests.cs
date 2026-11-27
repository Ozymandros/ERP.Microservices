using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using MyApp.Auth.API.Permissions;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using Xunit;

namespace MyApp.Auth.API.Tests;

public class PermissionCheckerTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManager;
    private readonly Mock<IPermissionRepository> _permissionRepository = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();

    public PermissionCheckerTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private PermissionChecker CreateSut() =>
        new(_userManager.Object, _permissionRepository.Object, _httpContextAccessor.Object);

    [Fact]
    public async Task HasPermissionAsync_ByUserId_UserMissing_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        _userManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((ApplicationUser?)null);

        var result = await CreateSut().HasPermissionAsync(userId, "Users", "Read");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_ByUserId_DirectPermission_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, UserName = "alice" };
        _userManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _permissionRepository.Setup(r => r.GetByUserName("alice", "Users", "Read", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Permission(Guid.NewGuid()) { Module = "Users", Action = "Read" }]);

        var result = await CreateSut().HasPermissionAsync(userId, "Users", "Read");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_ByUserId_RolePermission_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, UserName = "bob" };
        _userManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _permissionRepository.Setup(r => r.GetByUserName("bob", "Billing", "Read", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Operator"]);
        _permissionRepository.Setup(r => r.GetByRoleName("Operator", "Billing", "Read", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Permission(Guid.NewGuid()) { Module = "Billing", Action = "Read" }]);

        var result = await CreateSut().HasPermissionAsync(userId, "Billing", "Read");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_CurrentUser_NoIdentity_ReturnsFalse()
    {
        var httpContext = new DefaultHttpContext();
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var result = await CreateSut().HasPermissionAsync("Users", "Read");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_CurrentUser_AdminRole_ReturnsTrue()
    {
        var claims = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")], "test"));
        var httpContext = new DefaultHttpContext { User = claims };
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "admin" };
        _userManager.Setup(m => m.FindByNameAsync("admin")).ReturnsAsync(user);
        _permissionRepository.Setup(r => r.GetByUserName("admin", "Agentic", "Read", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Admin"]);

        var result = await CreateSut().HasPermissionAsync("Agentic", "Read");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_EmptyModule_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CreateSut().HasPermissionAsync("", "Read"));
    }
}