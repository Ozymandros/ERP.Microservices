using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Infrastructure.Data;
using MyApp.Auth.Infrastructure.Data.Repositories;
using MyApp.Auth.Tests.Helpers;
using Xunit;

namespace MyApp.Auth.Tests.Repositories;

/// <summary>
/// Unit tests for Refresh Token Repository.
/// </summary>
public class RefreshTokenRepositoryTests
{
    private readonly AuthDbContext _context;
    private readonly RefreshTokenRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenRepositoryTests"/> class and seeds the in-memory database with test data.
    /// </summary>
    public RefreshTokenRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new RefreshTokenRepository(_context);
    }

    private ApplicationUser CreateTestUser()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            NormalizedUserName = "TESTUSER"
        };
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    #region GetByTokenAsync Tests

    /// <summary>
    /// Verifies that with valid token results in returns refresh token when Get By Token Async is called.
    /// </summary>
    [Fact]
    public async Task GetByTokenAsync_WithValidToken_ReturnsRefreshToken()
    {
        // Arrange
        var user = CreateTestUser();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "test-refresh-token-123",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.RefreshTokens.Add(refreshToken);
        _context.SaveChanges();

        // Act
        var result = await _repository.GetByTokenAsync("test-refresh-token-123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(refreshToken.Id, result.Id);
        Assert.Equal("test-refresh-token-123", result.Token);
    }

    /// <summary>
    /// Verifies that with non existent token results in returns null when Get By Token Async is called.
    /// </summary>
    [Fact]
    public async Task GetByTokenAsync_WithNonExistentToken_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByTokenAsync("non-existent-token");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that with empty token results in returns null when Get By Token Async is called.
    /// </summary>
    [Fact]
    public async Task GetByTokenAsync_WithEmptyToken_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByTokenAsync("");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByUserIdAsync Tests

    /// <summary>
    /// Verifies that with valid user id results in returns all user tokens when Get By User Id Async is called.
    /// </summary>
    [Fact]
    public async Task GetByUserIdAsync_WithValidUserId_ReturnsAllUserTokens()
    {
        // Arrange
        var user = CreateTestUser();
        var token1 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "token-1",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        var token2 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "token-2",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.RefreshTokens.AddRange(token1, token2);
        _context.SaveChanges();

        // Act
        var results = await _repository.GetByUserIdAsync(user.Id);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count());
        Assert.Contains(results, t => t.Token == "token-1");
        Assert.Contains(results, t => t.Token == "token-2");
    }

    /// <summary>
    /// Verifies that with no tokens results in returns empty list when Get By User Id Async is called.
    /// </summary>
    [Fact]
    public async Task GetByUserIdAsync_WithNoTokens_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var results = await _repository.GetByUserIdAsync(userId);

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    /// <summary>
    /// Verifies that Get By User Id Async returns both revoked and active tokens.
    /// </summary>
    [Fact]
    public async Task GetByUserIdAsync_ReturnsBothRevokedAndActiveTokens()
    {
        // Arrange
        var user = CreateTestUser();
        var activeToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "active-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        var revokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "revoked-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.RefreshTokens.AddRange(activeToken, revokedToken);
        _context.SaveChanges();

        // Act
        var results = await _repository.GetByUserIdAsync(user.Id);

        // Assert
        Assert.Equal(2, results.Count());
    }

    #endregion

    #region GetValidRefreshTokenAsync Tests

    /// <summary>
    /// Verifies that with valid non revoked token results in returns token when Get Valid Refresh Token Async is called.
    /// </summary>
    [Fact]
    public async Task GetValidRefreshTokenAsync_WithValidNonRevokedToken_ReturnsToken()
    {
        // Arrange
        var user = CreateTestUser();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "valid-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.RefreshTokens.Add(refreshToken);
        _context.SaveChanges();

        // Act
        var result = await _repository.GetValidRefreshTokenAsync(user.Id, "valid-token");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(refreshToken.Id, result.Id);
    }

    /// <summary>
    /// Verifies that with revoked token results in returns null when Get Valid Refresh Token Async is called.
    /// </summary>
    [Fact]
    public async Task GetValidRefreshTokenAsync_WithRevokedToken_ReturnsNull()
    {
        // Arrange
        var user = CreateTestUser();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "revoked-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.RefreshTokens.Add(refreshToken);
        _context.SaveChanges();

        // Act
        var result = await _repository.GetValidRefreshTokenAsync(user.Id, "revoked-token");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that with expired token results in returns null when Get Valid Refresh Token Async is called.
    /// </summary>
    [Fact]
    public async Task GetValidRefreshTokenAsync_WithExpiredToken_ReturnsNull()
    {
        // Arrange
        var user = CreateTestUser();
        var expiredToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "expired-token",
            ExpiresAt = DateTime.UtcNow.AddSeconds(-10), // Expired 10 seconds ago
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };
        _context.RefreshTokens.Add(expiredToken);
        _context.SaveChanges();

        // Act
        var result = await _repository.GetValidRefreshTokenAsync(user.Id, "expired-token");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that with wrong user id results in returns null when Get Valid Refresh Token Async is called.
    /// </summary>
    [Fact]
    public async Task GetValidRefreshTokenAsync_WithWrongUserId_ReturnsNull()
    {
        // Arrange
        var user1 = CreateTestUser();
        var user2 = CreateTestUser();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user1.Id,
            Token = "valid-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.RefreshTokens.Add(refreshToken);
        _context.SaveChanges();

        // Act
        var result = await _repository.GetValidRefreshTokenAsync(user2.Id, "valid-token");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CreateAsync Tests

    /// <summary>
    /// Verifies that with valid refresh token results in creates and returns token when Create Async is called.
    /// </summary>
    [Fact]
    public async Task CreateAsync_WithValidRefreshToken_CreatesAndReturnsToken()
    {
        // Arrange
        var user = CreateTestUser();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "new-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.CreateAsync(refreshToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(refreshToken.Id, result.Id);
        var savedToken = _context.RefreshTokens.Find(result.Id);
        Assert.NotNull(savedToken);
        Assert.Equal("new-token", savedToken.Token);
    }

    #endregion

    #region RevokeAsync Tests

    /// <summary>
    /// Verifies that with valid token id results in revokes single token when Revoke Async is called.
    /// </summary>
    [Fact]
    public async Task RevokeAsync_WithValidTokenId_RevokesSingleToken()
    {
        // Arrange
        var user = CreateTestUser();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "token-to-revoke",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.RefreshTokens.Add(refreshToken);
        _context.SaveChanges();

        // Act
        await _repository.RevokeAsync(refreshToken.Id);

        // Assert
        var revokedToken = _context.RefreshTokens.Find(refreshToken.Id);
        Assert.NotNull(revokedToken);
        Assert.True(revokedToken.IsRevoked);
    }

    /// <summary>
    /// Verifies that with non existent token id results in does not throw when Revoke Async is called.
    /// </summary>
    [Fact]
    public async Task RevokeAsync_WithNonExistentTokenId_DoesNotThrow()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await _repository.RevokeAsync(nonExistentId); // Should not throw
    }

    /// <summary>
    /// Verifies that Revoke Async only revokes specific token.
    /// </summary>
    [Fact]
    public async Task RevokeAsync_OnlyRevokesSpecificToken()
    {
        // Arrange
        var user = CreateTestUser();
        var token1 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "token-1",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        var token2 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "token-2",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.RefreshTokens.AddRange(token1, token2);
        _context.SaveChanges();

        // Act
        await _repository.RevokeAsync(token1.Id);

        // Assert
        var revokedToken = _context.RefreshTokens.Find(token1.Id);
        var activeToken = _context.RefreshTokens.Find(token2.Id);
        Assert.True(revokedToken?.IsRevoked);
        Assert.False(activeToken?.IsRevoked);
    }

    #endregion

    #region RevokeUserTokensAsync Tests

    /// <summary>
    /// Verifies that with valid user id results in revokes all user tokens when Revoke User Tokens Async is called.
    /// </summary>
    [Fact]
    public async Task RevokeUserTokensAsync_WithValidUserId_RevokesAllUserTokens()
    {
        // Arrange
        var user = CreateTestUser();
        var token1 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "token-1",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        var token2 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "token-2",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.RefreshTokens.AddRange(token1, token2);
        _context.SaveChanges();

        // Act
        await _repository.RevokeUserTokensAsync(user.Id);

        // Assert
        var allUserTokens = _context.RefreshTokens.Where(rt => rt.UserId == user.Id).ToList();
        Assert.All(allUserTokens, token => Assert.True(token.IsRevoked));
    }

    /// <summary>
    /// Verifies that Revoke User Tokens Async does not affect other users tokens.
    /// </summary>
    [Fact]
    public async Task RevokeUserTokensAsync_DoesNotAffectOtherUsersTokens()
    {
        // Arrange
        var user1 = CreateTestUser();
        var user2 = CreateTestUser();
        var user1Token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user1.Id,
            Token = "user1-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        var user2Token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user2.Id,
            Token = "user2-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.RefreshTokens.AddRange(user1Token, user2Token);
        _context.SaveChanges();

        // Act
        await _repository.RevokeUserTokensAsync(user1.Id);

        // Assert
        var user1RevokedToken = _context.RefreshTokens.Find(user1Token.Id);
        var user2ActiveToken = _context.RefreshTokens.Find(user2Token.Id);
        Assert.True(user1RevokedToken?.IsRevoked);
        Assert.False(user2ActiveToken?.IsRevoked);
    }

    /// <summary>
    /// Verifies that with already revoked tokens results in keeps them revoked when Revoke User Tokens Async is called.
    /// </summary>
    [Fact]
    public async Task RevokeUserTokensAsync_WithAlreadyRevokedTokens_KeepsThemRevoked()
    {
        // Arrange
        var user = CreateTestUser();
        var revokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "revoked-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.RefreshTokens.Add(revokedToken);
        _context.SaveChanges();

        // Act
        await _repository.RevokeUserTokensAsync(user.Id);

        // Assert
        var token = _context.RefreshTokens.Find(revokedToken.Id);
        Assert.True(token?.IsRevoked);
    }

    #endregion
}
