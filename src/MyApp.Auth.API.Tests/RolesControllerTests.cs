using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using MyApp.Auth.API.Controllers;
using MyApp.Auth.Application.Contracts;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Application.Contracts.Services;
using MyApp.Auth.Domain.Specifications;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Security;
using Xunit;

namespace MyApp.Auth.API.Tests;

public class RolesControllerTests
{
    private readonly Mock<IRoleService> _roleServiceMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly Mock<IPermissionService> _permissionServiceMock = new();
    private readonly Mock<ILogSanitizer> _logSanitizerMock = new();
    private readonly Mock<ILogger<RolesController>> _loggerMock = new();
    private readonly RolesController _controller;

    public RolesControllerTests()
    {
        _logSanitizerMock.Setup(s => s.Sanitize(It.IsAny<string?>())).Returns<string?>(v => v ?? string.Empty);

        _controller = new RolesController(
            _roleServiceMock.Object,
            _loggerMock.Object,
            _cacheServiceMock.Object,
            _permissionServiceMock.Object,
            _logSanitizerMock.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetAll_WithoutQueryParams_ReturnsCachedRoles()
    {
        var roles = new List<RoleDto> { new(Guid.NewGuid()) { Name = "Admin" } };
        _cacheServiceMock.Setup(c => c.GetStateAsync<IEnumerable<RoleDto>>("all_roles"))
            .ReturnsAsync(roles);

        var result = await _controller.GetAll(new QuerySpec());

        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsAssignableFrom<IEnumerable<RoleDto>>(ok.Value);
        Assert.Single(returned);
        _roleServiceMock.Verify(r => r.GetAllRolesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAll_WithQueryParams_CallsQueryRolesAsync()
    {
        _controller.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            { "SearchTerm", "admin" }
        });

        var paginated = new PaginatedResult<RoleDto>([], 1, 10, 0);
        _roleServiceMock.Setup(r => r.QueryRolesAsync(It.IsAny<RoleQuerySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginated);

        var result = await _controller.GetAll(new QuerySpec());

        Assert.IsType<OkObjectResult>(result);
        _roleServiceMock.Verify(r => r.QueryRolesAsync(It.IsAny<RoleQuerySpec>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenCached_ReturnsRoleWithoutCallingService()
    {
        var roleId = Guid.NewGuid();
        var role = new RoleDto(roleId) { Name = "Editor" };
        _cacheServiceMock.Setup(c => c.GetStateAsync<RoleDto>($"Role-{roleId}"))
            .ReturnsAsync(role);

        var result = await _controller.GetById(roleId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(role, ok.Value);
        _roleServiceMock.Verify(r => r.GetRoleByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNotFound()
    {
        var roleId = Guid.NewGuid();
        _cacheServiceMock.Setup(c => c.GetStateAsync<RoleDto>(It.IsAny<string>()))
            .ReturnsAsync((RoleDto?)null);
        _roleServiceMock.Setup(r => r.GetRoleByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleDto?)null);

        var result = await _controller.GetById(roleId);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddPermissionsToRole_WhenSuccess_ReturnsNoContentAndInvalidatesCaches()
    {
        var roleId = Guid.NewGuid();
        var permissionIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var role = new RoleDto(roleId) { Name = "Admin" };

        _roleServiceMock.Setup(r => r.GetRoleByIdAsync(roleId, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        _roleServiceMock.Setup(r => r.AddPermissionsToRole(It.IsAny<CreateRolePermissionsDto>()))
            .ReturnsAsync(true);

        var result = await _controller.AddPermissionsToRole(roleId, permissionIds);

        Assert.IsType<NoContentResult>(result);
        _cacheServiceMock.Verify(c => c.RemoveStateAsync($"Role-{roleId}"), Times.Once);
        _cacheServiceMock.Verify(c => c.RemoveStateAsync("all_roles"), Times.Once);
        _cacheServiceMock.Verify(c => c.RemoveStateAsync("all_permissions"), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddPermissionsToRole_WhenNoIds_BadRequest(bool useNull)
    {
        var roleId = Guid.NewGuid();
        IEnumerable<Guid>? ids = useNull ? null : Array.Empty<Guid>();

        var result = await _controller.AddPermissionsToRole(roleId, ids!);

        Assert.IsType<BadRequestObjectResult>(result);
        _roleServiceMock.Verify(r => r.AddPermissionsToRole(It.IsAny<CreateRolePermissionsDto>()), Times.Never);
    }

    [Fact]
    public async Task AddPermissionsToRole_WhenRoleMissing_ReturnsNotFound()
    {
        var roleId = Guid.NewGuid();
        _roleServiceMock.Setup(r => r.GetRoleByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleDto?)null);

        var result = await _controller.AddPermissionsToRole(roleId, [ Guid.NewGuid() ]);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AddPermissionsToRole_WhenServiceFails_Returns500()
    {
        var roleId = Guid.NewGuid();
        _roleServiceMock.Setup(r => r.GetRoleByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleDto(roleId) { Name = "Admin" });
        _roleServiceMock.Setup(r => r.AddPermissionsToRole(It.IsAny<CreateRolePermissionsDto>()))
            .ReturnsAsync(false);

        var result = await _controller.AddPermissionsToRole(roleId, [ Guid.NewGuid() ]);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task RemovePermissionsFromRole_WhenSuccess_ReturnsNoContent()
    {
        var roleId = Guid.NewGuid();
        var permissionIds = new[] { Guid.NewGuid() };
        _roleServiceMock.Setup(r => r.GetRoleByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleDto(roleId) { Name = "Admin" });
        _roleServiceMock.Setup(r => r.RemovePermissionsFromRoleAsync(It.IsAny<DeleteRolePermissionsDto>()))
            .ReturnsAsync(true);

        var result = await _controller.RemovePermissionsFromRole(roleId, permissionIds);

        Assert.IsType<NoContentResult>(result);
        _cacheServiceMock.Verify(c => c.RemoveStateAsync($"Role-{roleId}"), Times.Once);
    }

    [Fact]
    public async Task AddPermissionToRole_WhenSuccess_InvalidatesUserCaches()
    {
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var role = new RoleDto(roleId) { Name = "Managers" };
        var permission = new PermissionDto(permissionId)
        {
            Module = "Billing",
            Action = "Read",
            Description = "Read billing"
        };

        _roleServiceMock.Setup(r => r.GetRoleByIdAsync(roleId, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        _permissionServiceMock.Setup(p => p.GetPermissionByIdAsync(permissionId, It.IsAny<CancellationToken>())).ReturnsAsync(permission);
        _roleServiceMock.Setup(r => r.AddPermissionToRole(It.IsAny<CreateRolePermissionDto>()))
            .ReturnsAsync(true);
        _roleServiceMock.Setup(r => r.GetUsersInRoleAsync("Managers", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserDto> { new(userId) { Email = "u@test.com" } });

        var result = await _controller.AddPermissionToRole(roleId, permissionId);

        Assert.IsType<NoContentResult>(result);
        _cacheServiceMock.Verify(c => c.RemoveStateAsync($"User-{userId}"), Times.Once);
    }

    [Fact]
    public async Task RemovePermissionFromRole_WhenAssociated_RemovesAndInvalidatesUserCaches()
    {
        var roleId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var role = new RoleDto(roleId) { Name = "Ops" };

        _roleServiceMock.Setup(r => r.HasPermissionAsync(roleId, permissionId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _roleServiceMock.Setup(r => r.GetRoleByIdAsync(roleId, It.IsAny<CancellationToken>())).ReturnsAsync(role);
        _roleServiceMock.Setup(r => r.RemovePermissionFromRoleAsync(It.IsAny<DeleteRolePermissionDto>()))
            .ReturnsAsync(true);
        _roleServiceMock.Setup(r => r.GetUsersInRoleAsync("Ops", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserDto> { new(userId) });

        var result = await _controller.RemovePermissionFromRole(roleId, permissionId);

        Assert.IsType<NoContentResult>(result);
        _cacheServiceMock.Verify(c => c.RemoveStateAsync($"User-{userId}"), Times.Once);
        _cacheServiceMock.Verify(c => c.RemoveStateAsync($"Roles-{userId}"), Times.Once);
    }
}