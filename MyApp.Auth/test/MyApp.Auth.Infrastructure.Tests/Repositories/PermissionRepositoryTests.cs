using Microsoft.AspNetCore.Identity;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Infrastructure.Data;
using MyApp.Auth.Infrastructure.Data.Repositories;
using MyApp.Auth.Tests.Helpers;
using Xunit;

namespace MyApp.Auth.Tests.Repositories;

public class PermissionRepositoryTests
{
    private readonly AuthDbContext _context;
    private readonly PermissionRepository _repository;

    public PermissionRepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext();
        _repository = new PermissionRepository(_context);
        TestDbContextFactory.SeedTestData(_context);
    }

    private Permission CreateTestPermission(string module, string action)
    {
        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Module = module,
            Action = action,
            Description = $"{module}.{action}"
        };
        _context.Permissions.Add(permission);
        _context.SaveChanges();
        return permission;
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

    private ApplicationUser CreateTestUser(string userName = "testuser")
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            NormalizedUserName = userName.ToUpper(),
            Email = $"{userName}@example.com",
            NormalizedEmail = $"{userName}@example.com".ToUpper()
        };
        _context.Users.Add(user);
        _context.SaveChanges();
        return user;
    }

    #region GetByRoleName Tests

    [Fact]
    public async Task GetByRoleName_WithValidRoleAndPermission_ReturnsPermissions()
    {
        // Arrange
        var role = CreateTestRole("Admin");
        var permission = CreateTestPermission("Inventory", "Read");

        var rolePermission = new RolePermission
        {
            Id = Guid.NewGuid(),
            Role = role,
            Permission = permission
        };
        _context.RolePermissions.Add(rolePermission);
        _context.SaveChanges();

        // Act
        var result = await _repository.GetByRoleName("Admin", "Inventory", "Read");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains(result, p => p.Module == "Inventory" && p.Action == "Read");
    }

    [Fact]
    public async Task GetByRoleName_WithNonExistentRole_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetByRoleName("NonExistentRole", "Module", "Action");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByRoleName_WithMismatchedModuleOrAction_ReturnsEmptyList()
    {
        // Arrange
        var role = CreateTestRole("Admin");
        var permission = CreateTestPermission("Inventory", "Read");

        var rolePermission = new RolePermission
        {
            Id = Guid.NewGuid(),
            Role = role,
            Permission = permission
        };
        _context.RolePermissions.Add(rolePermission);
        _context.SaveChanges();

        // Act
        var result = await _repository.GetByRoleName("Admin", "Sales", "Read");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByRoleName_WithNullParameters_ReturnsEmptyList()
    {
        // Act
        var result1 = await _repository.GetByRoleName(null!, "Module", "Action");
        var result2 = await _repository.GetByRoleName("Role", null!, "Action");
        var result3 = await _repository.GetByRoleName("Role", "Module", null!);

        // Assert
        Assert.Empty(result1);
        Assert.Empty(result2);
        Assert.Empty(result3);
    }

    [Fact]
    public async Task GetByRoleName_WithEmptyStringParameters_ReturnsEmptyList()
    {
        // Act
        var result1 = await _repository.GetByRoleName("", "Module", "Action");
        var result2 = await _repository.GetByRoleName("Role", "", "Action");
        var result3 = await _repository.GetByRoleName("Role", "Module", "");

        // Assert
        Assert.Empty(result1);
        Assert.Empty(result2);
        Assert.Empty(result3);
    }

    [Fact]
    public async Task GetByRoleName_WithMultiplePermissionsForRole_ReturnsCorrectPermissions()
    {
        // Arrange
        var role = CreateTestRole("Admin");
        var inventoryRead = CreateTestPermission("Inventory", "Read");
        var inventoryWrite = CreateTestPermission("Inventory", "Write");
        var salesRead = CreateTestPermission("Sales", "Read");

        _context.RolePermissions.AddRange(
            new RolePermission {Id = Guid.NewGuid(), Role = role, Permission = inventoryRead },
            new RolePermission {Id = Guid.NewGuid(), Role = role, Permission = inventoryWrite },
            new RolePermission {Id = Guid.NewGuid(), Role = role, Permission = salesRead }
        );
        _context.SaveChanges();

        // Act
        var result = await _repository.GetByRoleName("Admin", "Inventory", "Read");

        // Assert
        Assert.Single(result);
        Assert.Contains(result, p => p.Module == "Inventory" && p.Action == "Read");
    }

    #endregion

    #region GetByUserName Tests

    [Fact]
    public async Task GetByUserName_WithValidUserAndPermission_ReturnsPermissions()
    {
        // Arrange
        var user = CreateTestUser("john");
        var permission = CreateTestPermission("Orders", "Write");

        var userPermission = new UserPermission
        {
            Id = Guid.NewGuid(),
            User = user,
            Permission = permission,
        };
        _context.UserPermissions.Add(userPermission);
        _context.SaveChanges();

        // Act
        var result = await _repository.GetByUserName("john", "Orders", "Write");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains(result, p => p.Module == "Orders" && p.Action == "Write");
    }

    [Fact]
    public async Task GetByUserName_WithNonExistentUser_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetByUserName("NonExistentUser", "Module", "Action");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByUserName_WithMismatchedModuleOrAction_ReturnsEmptyList()
    {
        // Arrange
        var user = CreateTestUser("jane");
        var permission = CreateTestPermission("Purchasing", "Read");

        var userPermission = new UserPermission
        {
            Id = Guid.NewGuid(),
            User = user,
            Permission = permission
        };
        _context.UserPermissions.Add(userPermission);
        _context.SaveChanges();

        // Act
        var result = await _repository.GetByUserName("jane", "Billing", "Delete");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByUserName_WithNullParameters_ReturnsEmptyList()
    {
        // Act
        var result1 = await _repository.GetByUserName(null!, "Module", "Action");
        var result2 = await _repository.GetByUserName("User", null!, "Action");
        var result3 = await _repository.GetByUserName("User", "Module", null!);

        // Assert
        Assert.Empty(result1);
        Assert.Empty(result2);
        Assert.Empty(result3);
    }

    [Fact]
    public async Task GetByUserName_WithEmptyStringParameters_ReturnsEmptyList()
    {
        // Act
        var result1 = await _repository.GetByUserName("", "Module", "Action");
        var result2 = await _repository.GetByUserName("User", "", "Action");
        var result3 = await _repository.GetByUserName("User", "Module", "");

        // Assert
        Assert.Empty(result1);
        Assert.Empty(result2);
        Assert.Empty(result3);
    }

    [Fact]
    public async Task GetByUserName_WithMultiplePermissionsForUser_ReturnsCorrectPermission()
    {
        // Arrange
        var user = CreateTestUser("admin");
        var inventoryRead = CreateTestPermission("Inventory", "Read");
        var inventoryDelete = CreateTestPermission("Inventory", "Delete");
        var salesWrite = CreateTestPermission("Sales", "Write");

        _context.UserPermissions.AddRange(
            new UserPermission { Id = Guid.NewGuid(), User = user, Permission = inventoryRead },
            new UserPermission {Id = Guid.NewGuid(), User = user, Permission = inventoryDelete },
            new UserPermission {Id = Guid.NewGuid(), User = user, Permission = salesWrite }
        );
        _context.SaveChanges();

        // Act
        var result = await _repository.GetByUserName("admin", "Inventory", "Delete");

        // Assert
        Assert.Single(result);
        Assert.Contains(result, p => p.Module == "Inventory" && p.Action == "Delete");
    }

    [Fact]
    public async Task GetByUserName_WithDifferentCasings_ReturnsEmptyList()
    {
        // Arrange
        var user = CreateTestUser("testuser");
        var permission = CreateTestPermission("Inventory", "Read");

        var userPermission = new UserPermission
        {
            Id = Guid.NewGuid(),
            User = user,
            Permission = permission
        };
        _context.UserPermissions.Add(userPermission);
        _context.SaveChanges();

        // Act
        var result = await _repository.GetByUserName("TESTUSER", "Inventory", "Read");

        // Assert
        // Query is case-sensitive by default in EF Core
        Assert.Empty(result);
    }

    #endregion

    #region Base Repository Tests (GetById, GetAll, Add, Update, Delete)

    [Fact]
    public async Task GetByIdAsync_WithValidPermissionId_ReturnsPermission()
    {
        // Arrange
        var permission = CreateTestPermission("TestModule", "TestAction");

        // Act
        var result = await _repository.GetByIdAsync(permission.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(permission.Id, result.Id);
        Assert.Equal("TestModule", result.Module);
        Assert.Equal("TestAction", result.Action);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllPermissions()
    {
        // Arrange
        CreateTestPermission("Module1", "Action1");
        CreateTestPermission("Module2", "Action2");
        CreateTestPermission("Module3", "Action3");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count() >= 3);
    }

    [Fact]
    public async Task AddAsync_WithNewPermission_CreatesPermission()
    {
        // Arrange
        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Module = "NewModule",
            Action = "NewAction",
            Description = "New permission"
        };

        // Act
        await _repository.AddAsync(permission);
        var result = await _repository.GetByIdAsync(permission.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("NewModule", result.Module);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingPermission_UpdatesPermission()
    {
        // Arrange
        var permission = CreateTestPermission("OriginalModule", "OriginalAction");
        permission.Description = "Updated description";

        // Act
        await _repository.UpdateAsync(permission);
        var result = await _repository.GetByIdAsync(permission.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated description", result.Description);
    }

    [Fact]
    public async Task DeleteAsync_WithValidPermissionId_DeletesPermission()
    {
        // Arrange
        var permission = CreateTestPermission("ToDelete", "ToDelete");

        // Act
        await _repository.DeleteAsync(permission);
        var result = await _repository.GetByIdAsync(permission.Id);

        // Assert
        Assert.Null(result);
    }

    #endregion
}
