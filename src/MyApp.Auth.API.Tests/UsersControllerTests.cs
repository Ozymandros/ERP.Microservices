using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Auth.API.Controllers;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Security;
using Xunit;

namespace MyApp.Auth.API.Tests;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<ILogSanitizer> _logSanitizer = new();
    private readonly Mock<ILogger<UsersController>> _logger = new();
    private readonly UsersController _sut;

    public UsersControllerTests()
    {
        _sut = new UsersController(_userService.Object, _logger.Object, _cacheService.Object, _logSanitizer.Object);
        _sut.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [Fact]
    public async Task GetAll_CacheHit_ReturnsCachedUsers()
    {
        var cached = new List<UserDto> { new(Guid.NewGuid()) { Email = "a@test.com", Username = "usera" } };
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<UserDto>>("all_users")).ReturnsAsync(cached);

        var result = await _sut.GetAll(new QuerySpec(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(cached);
        _userService.Verify(s => s.GetAllUsersAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAll_CacheMiss_CallsService()
    {
        var users = new List<UserDto> { new(Guid.NewGuid()) { Username = "u1" } };
        _cacheService.Setup(c => c.GetStateAsync<IEnumerable<UserDto>>("all_users"))
            .ReturnsAsync((IEnumerable<UserDto>?)null);
        _userService.Setup(s => s.GetAllUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

        var result = await _sut.GetAll(new QuerySpec(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _userService.Verify(s => s.GetAllUsersAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _cacheService.Setup(c => c.GetStateAsync<UserDto>($"User-{id}")).ReturnsAsync((UserDto?)null);
        _userService.Setup(s => s.GetUserByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((UserDto?)null);

        var result = await _sut.GetById(id, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_Exists_ReturnsUser()
    {
        var id = Guid.NewGuid();
        var user = new UserDto(id) { Username = "found" };
        _cacheService.Setup(c => c.GetStateAsync<UserDto>($"User-{id}")).ReturnsAsync((UserDto?)null);
        _userService.Setup(s => s.GetUserByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _cacheService.Setup(c => c.SaveStateAsync(It.IsAny<string>(), user, It.IsAny<TimeSpan?>())).Returns(Task.CompletedTask);

        var result = await _sut.GetById(id, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(user);
    }
}