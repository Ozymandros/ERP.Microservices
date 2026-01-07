using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Application.Services;
using MyApp.Auth.Application.Tests.Common;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using MyApp.Auth.Infrastructure.Services;
using System.Security.Claims;
using Xunit;

namespace MyApp.Auth.Application.Tests.Services;

public class AuthServiceTests : BaseServiceTest
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IJwtTokenProvider> _mockJwtTokenProvider;
    private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AuthService _authService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;

    public AuthServiceTests()
    {
        _mockUserManager = CreateMockUserManager();
        _mockJwtTokenProvider = new Mock<IJwtTokenProvider>();
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _mockLogger = CreateMockLogger<AuthService>();
        _mockMapper = new Mock<IMapper>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockSignInManager = CreateMockSignInManager(_mockUserManager.Object);

        _authService = new AuthService(
            _mockUserManager.Object,
            _mockJwtTokenProvider.Object,
            _mockRefreshTokenRepository.Object,
            _mockUserRepository.Object,
            _mockLogger.Object);
    }

    #region LoginAsync

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnTokenResponse()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "test@example.com", Password = "ValidPassword123!" };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = loginDto.Email,
            UserName = loginDto.Email,
            EmailConfirmed = true
        };

        var tokenResponse = new TokenResponseDto("access_token", "refresh_token", 3600, "Bearer", null);

        _mockUserManager
            .Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);

        _mockSignInManager
            .Setup(x => x.CheckPasswordSignInAsync(user, loginDto.Password, false))
            .ReturnsAsync(SignInResult.Success);

        MockMapper
            .Setup(x => x.Map<TokenResponseDto>(It.IsAny<object>()))
            .Returns(tokenResponse);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(tokenResponse);
        _mockUserManager.Verify(x => x.FindByEmailAsync(loginDto.Email), Times.Once);
        _mockSignInManager.Verify(x => x.CheckPasswordSignInAsync(user, loginDto.Password, false), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ShouldReturnNull()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "nonexistent@example.com", Password = "ValidPassword123!" };

        _mockUserManager
            .Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        result.Should().BeNull();
        _mockSignInManager.Verify(x => x.CheckPasswordSignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldReturnNull()
    {
        // Arrange
        var loginDto = new LoginDto
        (
            Email: "test@example.com",
            Password: "InvalidPassword"
        );

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = loginDto.Email,
            UserName = loginDto.Email,
            EmailConfirmed = true
        };

        _mockUserManager
            .Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);

        _mockSignInManager
            .Setup(x => x.CheckPasswordSignInAsync(user, loginDto.Password, false))
            .ReturnsAsync(SignInResult.Failed);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        result.Should().BeNull();
        VerifyLoggerCalled(_mockLogger, LogLevel.Warning, Times.Once());
    }

    [Fact]
    public async Task LoginAsync_WithLockedOutUser_ShouldReturnNullAndLogWarning()
    {
        // Arrange
        var loginDto = new LoginDto
        (
            Email: "test@example.com",
            Password: "ValidPassword123!"
        );

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = loginDto.Email,
            UserName = loginDto.Email
        };

        _mockUserManager
            .Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);

        _mockSignInManager
            .Setup(x => x.CheckPasswordSignInAsync(user, loginDto.Password, false))
            .ReturnsAsync(SignInResult.LockedOut);

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        result.Should().BeNull();
        VerifyLoggerCalled(_mockLogger, LogLevel.Warning, Times.Once());
    }

    #endregion

    #region RegisterAsync

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldReturnTokenResponse()
    {
        // Arrange
        var registerDto = new RegisterDto("newuser@example.com", "newuser", "ValidPassword123!", "ValidPassword123!", "John", "Doe");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = registerDto.Email,
            UserName = registerDto.Email,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName
        };

        var tokenResponse = new TokenResponseDto("access_token", "refresh_token", 3600, "Bearer", null);

        _mockUserManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
            .ReturnsAsync(IdentityResult.Success);

        MockMapper
            .Setup(x => x.Map<TokenResponseDto>(It.IsAny<object>()))
            .Returns(tokenResponse);

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(tokenResponse);
        _mockUserManager.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u =>
            u.Email == registerDto.Email &&
            u.FirstName == registerDto.FirstName &&
            u.LastName == registerDto.LastName), registerDto.Password), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldReturnNull()
    {
        // Arrange
        var registerDto = new RegisterDto("existing@example.com", "username", "ValidPassword123!", "ValidPassword123!");

        var identityErrors = new List<IdentityError>
        {
            new IdentityError { Code = "DuplicateEmail", Description = "Email already exists" }
        };

        _mockUserManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
            .ReturnsAsync(IdentityResult.Failed(identityErrors.ToArray()));

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        result.Should().BeNull();
        VerifyLoggerCalled(_mockLogger, LogLevel.Warning, Times.Once());
    }

    #endregion

    #region RefreshTokenAsync

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ShouldReturnNewTokenResponse()
    {
        // Arrange
        var refreshToken = "valid token";
        var userId = Guid.NewGuid();

        var storedRefreshToken = new RefreshTokenDto(refreshToken, refreshToken);

        var user = new ApplicationUser
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "test@example.com"
        };

        var tokenResponse = new TokenResponseDto("new_access_token", "new_refresh_token", 3600);

        //_mockRefreshTokenRepository
        //    .Setup(x => x.GetByTokenAsync(refreshToken))
        //    .ReturnsAsync(storedRefreshToken);

        _mockUserManager
            .Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        MockMapper
            .Setup(x => x.Map<TokenResponseDto>(It.IsAny<object>()))
            .Returns(tokenResponse);

        // Act
        var result = await _authService.RefreshTokenAsync(storedRefreshToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(tokenResponse);
        //_mockRefreshTokenRepository.Verify(x => x.RevokeAsync(refreshToken), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var refreshToken = "invalid_refresh_token";
        var storedRefreshToken = new RefreshTokenDto(refreshToken, refreshToken);

        _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenAsync(refreshToken))
            .ReturnsAsync((RefreshToken?)null);

        // Act
        var result = await _authService.RefreshTokenAsync(storedRefreshToken);

        // Assert
        result.Should().BeNull();
        VerifyLoggerCalled(_mockLogger, LogLevel.Warning, Times.Once());
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ShouldReturnNull()
    {
        // Arrange
        var refreshToken = "expired_refresh_token";

        var storedRefreshToken = new RefreshTokenDto(refreshToken, refreshToken);

        //_mockRefreshTokenRepository
        //    .Setup(x => x.GetByTokenAsync(refreshToken).Result)
        //    .ReturnsAsync(storedRefreshToken);

        // Act
        var result = await _authService.RefreshTokenAsync(storedRefreshToken);

        // Assert
        result.Should().BeNull();
        VerifyLoggerCalled(_mockLogger, LogLevel.Warning, Times.Once());
    }

    #endregion

    #region LogoutAsync

    [Fact]
    public async Task LogoutAsync_WithValidRefreshToken_ShouldRevokeToken()
    {
        // Arrange
        var refreshToken = Guid.NewGuid();

        _mockRefreshTokenRepository
            .Setup(x => x.RevokeAsync(refreshToken))
            .Returns(Task.CompletedTask);

        // Act
        await _authService.LogoutAsync(refreshToken);

        // Assert
        _mockRefreshTokenRepository.Verify(x => x.RevokeAsync(refreshToken), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_WithRepositoryException_ShouldLogError()
    {
        // Arrange
        var refreshToken = Guid.NewGuid();
        var exception = new Exception("Database error");

        _mockRefreshTokenRepository
            .Setup(x => x.RevokeAsync(refreshToken))
            .ThrowsAsync(exception);

        // Act
        await _authService.LogoutAsync(refreshToken);

        // Assert
        VerifyLoggerCalled(_mockLogger, LogLevel.Error, Times.Once());
    }

    #endregion

    #region ExternalLoginAsync

    [Fact]
    public async Task ExternalLoginAsync_WithValidExternalUser_ShouldReturnTokenResponse()
    {
        // Arrange
        var externalLoginDto = new ExternalLoginDto("Google", "google_user_id", "external@example.com", "External ApplicationUser", "User");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = externalLoginDto.Email,
            UserName = externalLoginDto.Email
        };

        var tokenResponse = new TokenResponseDto("access_token", "refresh_token", 3600);

        //_mockUserManager
        //    .Setup(x => x.FindByLoginAsync(externalLoginDto.Provider, externalLoginDto.ProviderKey))
        //    .ReturnsAsync(user);

        MockMapper
            .Setup(x => x.Map<TokenResponseDto>(It.IsAny<object>()))
            .Returns(tokenResponse);

        // Act
        var result = await _authService.ExternalLoginAsync(externalLoginDto);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(tokenResponse);
    }

    [Fact]
    public async Task ExternalLoginAsync_WithNewExternalUser_ShouldCreateUserAndReturnTokenResponse()
    {
        // Arrange
        var externalLoginDto = new ExternalLoginDto("Google", "google_user_id", "newexternal@example.com", "New External ApplicationUser");

        var tokenResponse = new TokenResponseDto("access_token", "refresh_token", 3600);

        //_mockUserManager
        //    .Setup(x => x.FindByLoginAsync(externalLoginDto.Provider, externalLoginDto.ProviderKey))
        //    .ReturnsAsync((ApplicationUser?)null);

        _mockUserManager
            .Setup(x => x.FindByEmailAsync(externalLoginDto.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _mockUserManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager
            .Setup(x => x.AddLoginAsync(It.IsAny<ApplicationUser>(), It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Success);

        MockMapper
            .Setup(x => x.Map<TokenResponseDto>(It.IsAny<object>()))
            .Returns(tokenResponse);

        // Act
        var result = await _authService.ExternalLoginAsync(externalLoginDto);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(tokenResponse);
        _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Once);
        _mockUserManager.Verify(x => x.AddLoginAsync(It.IsAny<ApplicationUser>(), It.IsAny<UserLoginInfo>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static Mock<UserManager<ApplicationUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static Mock<SignInManager<ApplicationUser>> CreateMockSignInManager(UserManager<ApplicationUser> userManager)
    {
        var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        return new Mock<SignInManager<ApplicationUser>>(userManager, contextAccessor.Object, claimsFactory.Object, null!, null!, null!, null!);
    }

    #endregion
}