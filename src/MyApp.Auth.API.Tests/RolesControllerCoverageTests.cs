using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Auth.API.Controllers;
using MyApp.Auth.Application.Contracts;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Security;
using Xunit;

namespace MyApp.Auth.API.Tests;

/// <summary>
/// Additional <see cref="RolesController"/> tests covering export, CRUD cache invalidation
/// and bulk permission branches (non-collection enumerables, failures, information logging).
/// </summary>
public class RolesControllerCoverageTests
{
    private const string AllRolesKey = "all_roles";

    private readonly Mock<IRoleService> _roleServiceMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly Mock<IPermissionService> _permissionServiceMock = new();
    private readonly Mock<ILogSanitizer> _logSanitizerMock = new();
    private readonly Mock<ILogger<RolesController>> _loggerMock = new();
    private readonly RolesController _controller;

    public RolesControllerCoverageTests()
    {
        _logSanitizerMock.Setup(s => s.Sanitize(It.IsAny<string?>())).Returns<string?>(v => v ?? string.Empty);
        // Enable Information so the IsEnabled-guarded log blocks execute.
        _loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        _controller = new RolesController(
            _roleServiceMock.Object,
            _loggerMock.Object,
            _cacheServiceMock.Object,
            _permissionServiceMock.Object,
            _logSanitizerMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private static IEnumerable<Guid> Lazy(params Guid[] ids)
    {
        foreach (var id in ids)
            yield return id;
    }

    private static List<RoleDto> SampleRoles() => [new RoleDto(Guid.NewGuid()) { Name = "Admin" }];

    #region Export

    [Fact]
    public async Task ExportToXlsx_WhenCacheMiss_UsesServiceAndReturnsFile()
    {
        _cacheServiceMock.Setup(c => c.GetStateAsync<IEnumerable<RoleDto>>(AllRolesKey))
            .ReturnsAsync((IEnumerable<RoleDto>?)null);
        _roleServiceMock.Setup(r => r.GetAllRolesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SampleRoles());

        var result = await _controller.ExportToXlsx();

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("Roles.xlsx", file.FileDownloadName);
        Assert.NotEmpty(file.FileContents);
    }

    [Fact]
    public async Task ExportToXlsx_WhenServiceThrows_Returns500()
    {
        _cacheServiceMock.Setup(c => c.GetStateAsync<IEnumerable<RoleDto>>(AllRolesKey))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _controller.ExportToXlsx();

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task ExportToPdf_WhenCached_ReturnsFileWithoutCallingService()
    {
        _cacheServiceMock.Setup(c => c.GetStateAsync<IEnumerable<RoleDto>>(AllRolesKey))
            .ReturnsAsync(SampleRoles());

        var result = await _controller.ExportToPdf();

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("Roles.pdf", file.FileDownloadName);
        _roleServiceMock.Verify(r => r.GetAllRolesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExportToPdf_WhenServiceThrows_Returns500()
    {
        _cacheServiceMock.Setup(c => c.GetStateAsync<IEnumerable<RoleDto>>(AllRolesKey))
            .ReturnsAsync((IEnumerable<RoleDto>?)null);
        _roleServiceMock.Setup(r => r.GetAllRolesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _controller.ExportToPdf();

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    #endregion

    #region GetAll / GetById

    [Fact]
    public async Task GetAll_WhenCacheMiss_LoadsFromServiceAndCaches()
    {
        var roles = SampleRoles();
        _cacheServiceMock.Setup(c => c.GetStateAsync<IEnumerable<RoleDto>>(AllRolesKey))
            .ReturnsAsync((IEnumerable<RoleDto>?)null);
        _roleServiceMock.Setup(r => r.GetAllRolesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(roles);

        var result = await _controller.GetAll(new QuerySpec());

        Assert.IsType<OkObjectResult>(result);
        _cacheServiceMock.Verify(c => c.SaveStateAsync<IEnumerable<RoleDto>>(AllRolesKey, roles, It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenCacheMiss_LoadsFromService()
    {
        var roleId = Guid.NewGuid();
        var role = new RoleDto(roleId) { Name = "Editor" };
        _cacheServiceMock.Setup(c => c.GetStateAsync<RoleDto>($"Role-{roleId}")).ReturnsAsync((RoleDto?)null);
        _roleServiceMock.Setup(r => r.GetRoleByIdAsync(roleId, It.IsAny<CancellationToken>())).ReturnsAsync(role);

        var result = await _controller.GetById(roleId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(role, ok.Value);
    }

    #endregion

    #region Create / Update / Delete

    [Fact]
    public async Task Create_WhenSuccess_InvalidatesAllRolesCache()
    {
        var created = new RoleDto(Guid.NewGuid()) { Name = "Auditor" };
        _roleServiceMock.Setup(r => r.CreateRoleAsync(It.IsAny<CreateRoleDto>())).ReturnsAsync(created);

        var result = await _controller.Create(new CreateRoleDto("Auditor"));

        Assert.IsType<CreatedAtActionResult>(result.Result);
        _cacheServiceMock.Verify(c => c.RemoveStateAsync(AllRolesKey), Times.Once);
    }

    [Fact]
    public async Task Create_WhenDuplicate_ReturnsConflict()
    {
        _roleServiceMock.Setup(r => r.CreateRoleAsync(It.IsAny<CreateRoleDto>())).ReturnsAsync((RoleDto?)null);

        var result = await _controller.Create(new CreateRoleDto("Auditor"));

        Assert.IsType<ConflictObjectResult>(result.Result);
        _cacheServiceMock.Verify(c => c.RemoveStateAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenSuccess_InvalidatesRoleCache()
    {
        var roleId = Guid.NewGuid();
        _roleServiceMock.Setup(r => r.UpdateRoleAsync(roleId, It.IsAny<CreateRoleDto>())).ReturnsAsync(true);

        var result = await _controller.Update(roleId, new CreateRoleDto("Renamed"));

        Assert.IsType<NoContentResult>(result);
        _cacheServiceMock.Verify(c => c.RemoveStateAsync($"Role-{roleId}"), Times.Once);
    }

    [Fact]
    public async Task Update_WhenMissing_ReturnsNotFound()
    {
        var roleId = Guid.NewGuid();
        _roleServiceMock.Setup(r => r.UpdateRoleAsync(roleId, It.IsAny<CreateRoleDto>())).ReturnsAsync(false);

        var result = await _controller.Update(roleId, new CreateRoleDto("Renamed"));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WhenSuccess_InvalidatesRoleAndAllRolesCache()
    {
        var roleId = Guid.NewGuid();
        _roleServiceMock.Setup(r => r.DeleteRoleAsync(roleId)).ReturnsAsync(true);

        var result = await _controller.Delete(roleId);

        Assert.IsType<NoContentResult>(result);
        _cacheServiceMock.Verify(c => c.RemoveStateAsync($"Role-{roleId}"), Times.Once);
        _cacheServiceMock.Verify(c => c.RemoveStateAsync(AllRolesKey), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenMissing_ReturnsNotFound()
    {
        var roleId = Guid.NewGuid();
        _roleServiceMock.Setup(r => r.DeleteRoleAsync(roleId)).ReturnsAsync(false);

        var result = await _controller.Delete(roleId);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region AddPermissionsToRole (bulk)

    [Fact]
    public async Task AddPermissionsToRole_WithNonCollectionEnumerable_CountsAndSucceeds()
    {
        var roleId = Guid.NewGuid();
        _roleServiceMock.Setup(r => r.GetRoleByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleDto(roleId) { Name = "Admin" });
        _roleServiceMock.Setup(r => r.AddPermissionsToRole(It.IsAny<CreateRolePermissionsDto>())).ReturnsAsync(true);

        var result = await _controller.AddPermissionsToRole(roleId, Lazy(Guid.NewGuid(), Guid.NewGuid()));

        Assert.IsType<NoContentResult>(result);
        _cacheServiceMock.Verify(c => c.RemoveStateAsync("all_permissions"), Times.Once);
    }

    [Fact]
    public async Task AddPermissionsToRole_WithEmptyNonCollectionEnumerable_ReturnsBadRequest()
    {
        var result = await _controller.AddPermissionsToRole(Guid.NewGuid(), Lazy());

        Assert.IsType<BadRequestObjectResult>(result);
        _roleServiceMock.Verify(r => r.GetRoleByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region RemovePermissionsFromRole (bulk)

    [Fact]
    public async Task RemovePermissionsFromRole_WhenNull_ReturnsBadRequest()
    {
        var result = await _controller.RemovePermissionsFromRole(Guid.NewGuid(), null!);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RemovePermissionsFromRole_WhenEmptyCollection_ReturnsBadRequest()
    {
        var result = await _controller.RemovePermissionsFromRole(Guid.NewGuid(), Array.Empty<Guid>());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RemovePermissionsFromRole_WhenEmptyNonCollectionEnumerable_ReturnsBadRequest()
    {
        var result = await _controller.RemovePermissionsFromRole(Guid.NewGuid(), Lazy());

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RemovePermissionsFromRole_WhenRoleMissing_ReturnsNotFound()
    {
        var roleId = Guid.NewGuid();
        _roleServiceMock.Setup(r => r.GetRoleByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleDto?)null);

        var result = await _controller.RemovePermissionsFromRole(roleId, [Guid.NewGuid()]);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task RemovePermissionsFromRole_WhenServiceFails_Returns500()
    {
        var roleId = Guid.NewGuid();
        _roleServiceMock.Setup(r => r.GetRoleByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleDto(roleId) { Name = "Admin" });
        _roleServiceMock.Setup(r => r.RemovePermissionsFromRoleAsync(It.IsAny<DeleteRolePermissionsDto>()))
            .ReturnsAsync(false);

        var result = await _controller.RemovePermissionsFromRole(roleId, [Guid.NewGuid()]);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
        _cacheServiceMock.Verify(c => c.RemoveStateAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RemovePermissionsFromRole_WithNonCollectionEnumerable_InvalidatesCaches()
    {
        var roleId = Guid.NewGuid();
        _roleServiceMock.Setup(r => r.GetRoleByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleDto(roleId) { Name = "Admin" });
        _roleServiceMock.Setup(r => r.RemovePermissionsFromRoleAsync(It.IsAny<DeleteRolePermissionsDto>()))
            .ReturnsAsync(true);

        var result = await _controller.RemovePermissionsFromRole(roleId, Lazy(Guid.NewGuid()));

        Assert.IsType<NoContentResult>(result);
        _cacheServiceMock.Verify(c => c.RemoveStateAsync($"Role-{roleId}"), Times.Once);
        _cacheServiceMock.Verify(c => c.RemoveStateAsync(AllRolesKey), Times.Once);
        _cacheServiceMock.Verify(c => c.RemoveStateAsync("all_permissions"), Times.Once);
    }

    #endregion
}
