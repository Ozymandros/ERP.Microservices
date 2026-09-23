using Microsoft.AspNetCore.Identity;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Infrastructure.Data;
using MyApp.Auth.Infrastructure.Data.Repositories;
using MyApp.Auth.Tests.Helpers;
using Xunit;

namespace MyApp.Auth.Tests.Repositories;

/// <summary>
/// Unit tests for User Repository.
/// </summary>
public class UserRepositoryTests
{
    private readonly AuthDbContext _context;
    private readonly UserRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepositoryTests"/> class and seeds the in-memory database with test users.
    /// </summary>
    public UserRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new UserRepository(_context);
        TestDbContextFactory.SeedTestData(_context);
    }

    private ApplicationUser CreateTestUser(string email = "test@example.com", string userName = "testuser")
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            NormalizedUserName = userName.ToUpper(),
            Email = email,
            NormalizedEmail = email.ToUpper(),
            EmailConfirmed = false,
            PasswordHash = "hashedpassword"
        };
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    #region GetByEmailAsync Tests

    /// <summary>
    /// Verifies that with valid email results in returns user when Get By Email Async is called.
    /// </summary>
    [Fact]
    public async Task GetByEmailAsync_WithValidEmail_ReturnsUser()
    {
        // Arrange
        var user = CreateTestUser("john@example.com", "john");

        // Act
        var result = await _repository.GetByEmailAsync("john@example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal("john@example.com", result.Email);
    }

    /// <summary>
    /// Verifies that with non existent email results in returns null when Get By Email Async is called.
    /// </summary>
    [Fact]
    public async Task GetByEmailAsync_WithNonExistentEmail_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByEmailAsync("nonexistent@example.com");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that with different casing results in returns null when Get By Email Async is called.
    /// </summary>
    [Fact]
    public async Task GetByEmailAsync_WithDifferentCasing_ReturnsNull()
    {
        // Arrange
        CreateTestUser("john@example.com", "john");

        // Act
        var result = await _repository.GetByEmailAsync("JOHN@EXAMPLE.COM");

        // Assert
        // Email is case-sensitive in EF Core without collation configuration
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that Get By Email Async includes refresh tokens.
    /// </summary>
    [Fact]
    public async Task GetByEmailAsync_IncludesRefreshTokens()
    {
        // Arrange
        var user = CreateTestUser("john@example.com", "john");
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.RefreshTokens.Add(refreshToken);
        _context.SaveChanges();

        // Act
        var result = await _repository.GetByEmailAsync("john@example.com");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.RefreshTokens);
        Assert.Single(result.RefreshTokens);
    }

    #endregion

    #region GetByExternalIdAsync Tests

    /// <summary>
    /// Verifies that with valid external provider results in returns user when Get By External Id Async is called.
    /// </summary>
    [Fact]
    public async Task GetByExternalIdAsync_WithValidExternalProvider_ReturnsUser()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "externaluser",
            Email = "external@example.com",
            ExternalProvider = "Google",
            ExternalId = "google-123456"
        };
        _context.Users.Add(user);
        _context.SaveChanges();

        // Act
        var result = await _repository.GetByExternalIdAsync("Google", "google-123456");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal("Google", result.ExternalProvider);
        Assert.Equal("google-123456", result.ExternalId);
    }

    /// <summary>
    /// Verifies that with non existent id results in returns null when Get By External Id Async is called.
    /// </summary>
    [Fact]
    public async Task GetByExternalIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByExternalIdAsync("Google", "nonexistent-id");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that with different provider results in returns null when Get By External Id Async is called.
    /// </summary>
    [Fact]
    public async Task GetByExternalIdAsync_WithDifferentProvider_ReturnsNull()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "externaluser",
            Email = "external@example.com",
            ExternalProvider = "Google",
            ExternalId = "google-123456"
        };
        _context.Users.Add(user);
        _context.SaveChanges();

        // Act
        var result = await _repository.GetByExternalIdAsync("Facebook", "google-123456");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByRoleAsync Tests

    /// <summary>
    /// Verifies that with valid role name results in returns users in role when Get By Role Async is called.
    /// </summary>
    [Fact]
    public async Task GetByRoleAsync_WithValidRoleName_ReturnsUsersInRole()
    {
        // Arrange
        var role = _context.Roles.FirstOrDefault(r => r.Name == "Admin");
        Assert.NotNull(role);

        var user1 = CreateTestUser("admin1@example.com", "admin1");
        var user2 = CreateTestUser("admin2@example.com", "admin2");
        var user3 = CreateTestUser("user@example.com", "regularuser");

        _context.UserRoles.AddRange(
            new ApplicationUserRole { UserId = user1.Id, RoleId = role.Id },
            new ApplicationUserRole { UserId = user2.Id, RoleId = role.Id }
        );
        _context.SaveChanges();

        // Act
        var result = await _repository.GetByRoleAsync("Admin");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, u => u.Id == user1.Id);
        Assert.Contains(result, u => u.Id == user2.Id);
        Assert.DoesNotContain(result, u => u.Id == user3.Id);
    }

    /// <summary>
    /// Verifies that with non existent role results in returns empty list when Get By Role Async is called.
    /// </summary>
    [Fact]
    public async Task GetByRoleAsync_WithNonExistentRole_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetByRoleAsync("NonExistentRole");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// Verifies that with no users in role results in returns empty list when Get By Role Async is called.
    /// </summary>
    [Fact]
    public async Task GetByRoleAsync_WithNoUsersInRole_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetByRoleAsync("User");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region EmailExistsAsync Tests

    /// <summary>
    /// Verifies that with existing email results in returns true when Email Exists Async is called.
    /// </summary>
    [Fact]
    public async Task EmailExistsAsync_WithExistingEmail_ReturnsTrue()
    {
        // Arrange
        CreateTestUser("existing@example.com", "existing");

        // Act
        var result = await _repository.EmailExistsAsync("existing@example.com");

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Verifies that with non existent email results in returns false when Email Exists Async is called.
    /// </summary>
    [Fact]
    public async Task EmailExistsAsync_WithNonExistentEmail_ReturnsFalse()
    {
        // Act
        var result = await _repository.EmailExistsAsync("nonexistent@example.com");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies that Email Exists Async is case sensitive.
    /// </summary>
    [Fact]
    public async Task EmailExistsAsync_IsCaseSensitive()
    {
        // Arrange
        CreateTestUser("test@example.com", "test");

        // Act
        var result = await _repository.EmailExistsAsync("TEST@EXAMPLE.COM");

        // Assert
        // Case-sensitive comparison by default in EF Core without collation
        Assert.False(result);
    }

    /// <summary>
    /// Verifies that with empty string results in returns false when Email Exists Async is called.
    /// </summary>
    [Fact]
    public async Task EmailExistsAsync_WithEmptyString_ReturnsFalse()
    {
        // Act
        var result = await _repository.EmailExistsAsync("");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Add Tests

    /// <summary>
    /// Verifies that with valid user results in creates user when Add Async is called.
    /// </summary>
    [Fact]
    public async Task AddAsync_WithValidUser_CreatesUser()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "newuser",
            Email = "new@example.com"
        };

        // Act
        await _repository.AddAsync(user);
        var result = _context.Users.Find(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newuser", result.UserName);
        Assert.Equal("new@example.com", result.Email);
    }

    #endregion

    #region Update Tests

    /// <summary>
    /// Verifies that with existing user results in updates user data when Update Async is called.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WithExistingUser_UpdatesUserData()
    {
        // Arrange
        var user = CreateTestUser("update@example.com", "updateuser");
        user.Email = "newemail@example.com";
        user.FirstName = "Updated";

        // Act
        await _repository.UpdateAsync(user);
        var result = _context.Users.Find(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newemail@example.com", result.Email);
        Assert.Equal("Updated", result.FirstName);
    }

    #endregion

    #region Delete Tests

    /// <summary>
    /// Verifies that with valid user id results in deletes user when Delete Async is called.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_WithValidUserId_DeletesUser()
    {
        // Arrange
        var user = CreateTestUser("delete@example.com", "deleteuser");

        // Act
        await _repository.DeleteAsync(user);
        await _context.SaveChangesAsync();
        var result = _context.Users.Find(user.Id);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAll Tests

    /// <summary>
    /// Verifies that Get All Async returns all users.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        // Arrange
        CreateTestUser("user1@example.com", "user1");
        CreateTestUser("user2@example.com", "user2");
        CreateTestUser("user3@example.com", "user3");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count() >= 3);
    }

    #endregion
}
