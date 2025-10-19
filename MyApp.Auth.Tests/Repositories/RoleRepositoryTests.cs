using Microsoft.AspNetCore.Identity;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Infrastructure.Data;
using MyApp.Auth.Infrastructure.Data.Repositories;
using MyApp.Auth.Tests.Helpers;
using Xunit;

namespace MyApp.Auth.Tests.Repositories;

public class RoleRepositoryTests
{
    private readonly AuthDbContext _context;
    private readonly RoleRepository _repository;

    public RoleRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new RoleRepository(_context);
    }

    private ApplicationRole CreateTestRole(string name)
    {
        var role = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = name,
            NormalizedName = name.ToUpper()
        };
        _context.Roles.Add(role);
        _context.SaveChanges();
        return role;
    }

    private ApplicationUser CreateTestUser(string userName = "testuser", string email = "test@example.com")
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            NormalizedUserName = userName.ToUpper(),
            Email = email,
            NormalizedEmail = email.ToUpper()
        };
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    #region GetByNameAsync Tests

    [Fact]
    public async Task GetByNameAsync_WithValidRoleName_ReturnsRole()
    {
        // Arrange
        var role = CreateTestRole("Admin");

        // Act
        var result = await _repository.GetByNameAsync("Admin");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(role.Id, result.Id);
        Assert.Equal("Admin", result.Name);
    }

    [Fact]
    public async Task GetByNameAsync_WithNonExistentRole_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByNameAsync("NonExistentRole");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByNameAsync_IsCaseSensitive()
    {
        // Arrange
        CreateTestRole("Admin");

        // Act
        var result = await _repository.GetByNameAsync("admin");

        // Assert
        // By default EF Core queries are case-sensitive on Linux/Mac, 
        // but this depends on database collation
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByNameAsync_IncludesRoleClaims()
    {
        // Arrange
        var role = CreateTestRole("Admin");
        var claim = new IdentityRoleClaim<Guid>
        {
            RoleId = role.Id,
            ClaimType = "Permission",
            ClaimValue = "Admin.Read"
        };
        _context.RoleClaims.Add(claim);
        _context.SaveChanges();

        // Act
        var result = await _repository.GetByNameAsync("Admin");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.RoleClaims);
        Assert.Single(result.RoleClaims);
    }

    #endregion

    #region NameExistsAsync Tests

    [Fact]
    public async Task NameExistsAsync_WithExistingRole_ReturnsTrue()
    {
        // Arrange
        CreateTestRole("Existing");

        // Act
        var result = await _repository.NameExistsAsync("Existing");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task NameExistsAsync_WithNonExistentRole_ReturnsFalse()
    {
        // Act
        var result = await _repository.NameExistsAsync("NonExistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task NameExistsAsync_WithEmptyString_ReturnsFalse()
    {
        // Act
        var result = await _repository.NameExistsAsync("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task NameExistsAsync_WithMultipleRoles_ChecksCorrectly()
    {
        // Arrange
        CreateTestRole("Admin");
        CreateTestRole("User");
        CreateTestRole("Manager");

        // Act
        var adminExists = await _repository.NameExistsAsync("Admin");
        var userExists = await _repository.NameExistsAsync("User");
        var managerExists = await _repository.NameExistsAsync("Manager");
        var nonExistentExists = await _repository.NameExistsAsync("NonExistent");

        // Assert
        Assert.True(adminExists);
        Assert.True(userExists);
        Assert.True(managerExists);
        Assert.False(nonExistentExists);
    }

    #endregion

    #region GetRolesByUserIdAsync Tests

    [Fact]
    public async Task GetRolesByUserIdAsync_WithUserInRoles_ReturnsUserRoles()
    {
        // Arrange
        var adminRole = CreateTestRole("Admin");
        var managerRole = CreateTestRole("Manager");
        var user = CreateTestUser();

        _context.UserRoles.AddRange(
            new IdentityUserRole<Guid> { UserId = user.Id, RoleId = adminRole.Id },
            new IdentityUserRole<Guid> { UserId = user.Id, RoleId = managerRole.Id }
        );
        _context.SaveChanges();

        // Act
        var result = await _repository.GetRolesByUserIdAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, r => r.Name == "Admin");
        Assert.Contains(result, r => r.Name == "Manager");
    }

    [Fact]
    public async Task GetRolesByUserIdAsync_WithUserNotInRoles_ReturnsEmptyList()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var result = await _repository.GetRolesByUserIdAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRolesByUserIdAsync_WithNonExistentUserId_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var result = await _repository.GetRolesByUserIdAsync(nonExistentUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRolesByUserIdAsync_IncludesRoleClaims()
    {
        // Arrange
        var adminRole = CreateTestRole("Admin");
        var user = CreateTestUser();

        _context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = adminRole.Id });

        var claim = new IdentityRoleClaim<Guid>
        {
            RoleId = adminRole.Id,
            ClaimType = "Permission",
            ClaimValue = "Admin.Write"
        };
        _context.RoleClaims.Add(claim);
        _context.SaveChanges();

        // Act
        var result = await _repository.GetRolesByUserIdAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        var roleWithClaims = result.First();
        Assert.NotNull(roleWithClaims.RoleClaims);
        Assert.Single(roleWithClaims.RoleClaims);
    }

    [Fact]
    public async Task GetRolesByUserIdAsync_WithMultipleUsers_ReturnOnlySpecificUserRoles()
    {
        // Arrange
        var adminRole = CreateTestRole("Admin");
        var userRole = CreateTestRole("User");
        var user1 = CreateTestUser("user1");
        var user2 = CreateTestUser("user2");

        _context.UserRoles.AddRange(
            new IdentityUserRole<Guid> { UserId = user1.Id, RoleId = adminRole.Id },
            new IdentityUserRole<Guid> { UserId = user2.Id, RoleId = userRole.Id }
        );
        _context.SaveChanges();

        // Act
        var user1Roles = await _repository.GetRolesByUserIdAsync(user1.Id);
        var user2Roles = await _repository.GetRolesByUserIdAsync(user2.Id);

        // Assert
        Assert.Single(user1Roles);
        Assert.Contains(user1Roles, r => r.Name == "Admin");
        Assert.Single(user2Roles);
        Assert.Contains(user2Roles, r => r.Name == "User");
    }

    #endregion

    #region Add Tests

    [Fact]
    public async Task AddAsync_WithValidRole_CreatesRole()
    {
        // Arrange
        var role = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = "NewRole",
            NormalizedName = "NEWROLE"
        };

        // Act
        await _repository.AddAsync(role);
        var result = _context.Roles.Find(role.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("NewRole", result.Name);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task UpdateAsync_WithExistingRole_UpdatesRoleData()
    {
        // Arrange
        var role = CreateTestRole("Original");
        role.Name = "Updated";

        // Act
        await _repository.UpdateAsync(role);
        var result = _context.Roles.Find(role.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated", result.Name);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteAsync_WithValidRoleId_DeletesRole()
    {
        // Arrange
        var role = CreateTestRole("ToDelete");

        // Act
        await _repository.DeleteAsync(role);
        var result = _context.Roles.Find(role.Id);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllRoles()
    {
        // Arrange
        CreateTestRole("Admin");
        CreateTestRole("User");
        CreateTestRole("Manager");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count() >= 3);
    }

    #endregion
}
