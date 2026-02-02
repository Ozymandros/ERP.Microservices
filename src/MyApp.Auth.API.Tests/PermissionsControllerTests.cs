using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using MyApp.Auth.API.Controllers;
using MyApp.Auth.Application.Contracts;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Domain.Specifications;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;
using Xunit;

namespace MyApp.Auth.API.Tests;

public class PermissionsControllerTests
{
    private readonly Mock<IPermissionService> _permissionServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<PermissionsController>> _loggerMock;
    private readonly PermissionsController _controller;

    public PermissionsControllerTests()
    {
        _permissionServiceMock = new Mock<IPermissionService>();
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<PermissionsController>>();

        _controller = new PermissionsController(
            _permissionServiceMock.Object,
            _cacheServiceMock.Object,
            _loggerMock.Object);

        // Setup HttpContext
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetAll_WithoutQueryParams_ShouldReturnAllFromCache()
    {
        // Arrange
        var permissions = new List<PermissionDto> { new(Guid.NewGuid()) { Module = "Module", Action = "Action", Description = "Desc" } };
        _cacheServiceMock.Setup(x => x.GetStateAsync<IEnumerable<PermissionDto>>("all_permissions"))
            .ReturnsAsync(permissions);

        // Act
        var result = await _controller.GetAll(new QuerySpec());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPermissions = Assert.IsAssignableFrom<IEnumerable<PermissionDto>>(okResult.Value);
        Assert.Single(returnedPermissions);
        _permissionServiceMock.Verify(x => x.QueryPermissionsAsync(It.IsAny<PermissionQuerySpec>()), Times.Never);
    }

    [Fact]
    public async Task GetAll_WithQueryParams_ShouldCallQueryPermissionsAsync()
    {
        // Arrange
        var querySpec = new QuerySpec();
        // Manually add a query parameter to HttpContext
        _controller.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            { "SearchTerm", "test" }
        });

        var paginatedResult = new PaginatedResult<PermissionDto>(
            new List<PermissionDto>(), 1, 10, 0);

        _permissionServiceMock.Setup(x => x.QueryPermissionsAsync(It.IsAny<PermissionQuerySpec>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _controller.GetAll(querySpec);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<PaginatedResult<PermissionDto>>(okResult.Value);
        _permissionServiceMock.Verify(x => x.QueryPermissionsAsync(It.IsAny<PermissionQuerySpec>()), Times.Once);
    }

    [Fact]
    public async Task Search_ShouldBindFiltersAndCallQueryPermissionsAsync()
    {
        // Arrange
        var querySpec = new QuerySpec();
        _controller.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            { "Module", "Users" }
        });

        var paginatedResult = new PaginatedResult<PermissionDto>(
            new List<PermissionDto>(), 1, 10, 0);

        _permissionServiceMock.Setup(x => x.QueryPermissionsAsync(It.IsAny<PermissionQuerySpec>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _controller.Search(querySpec);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<PaginatedResult<PermissionDto>>(okResult.Value);
        
        _permissionServiceMock.Verify(x => x.QueryPermissionsAsync(It.Is<PermissionQuerySpec>(s => 
            s.Query.Filters.ContainsKey("Module") && s.Query.Filters.GetValueOrDefault("Module") == "Users"
        )), Times.Once);
    }
}
