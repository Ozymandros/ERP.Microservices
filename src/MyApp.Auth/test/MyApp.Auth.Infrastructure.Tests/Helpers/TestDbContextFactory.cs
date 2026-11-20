using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Infrastructure.Data;

namespace MyApp.Auth.Tests.Helpers;

/// <summary>
/// Helper class for creating in-memory AuthDbContext instances for testing
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Creates a new <see cref="AuthDbContext"/> backed by a unique in-memory database and ensures the schema is created.
    /// </summary>
    /// <returns>A ready-to-use <see cref="AuthDbContext"/> instance.</returns>
    public static AuthDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AuthDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    /// <summary>
    /// Seeds the provided context with default Admin and User roles for integration tests.
    /// Clears any previously existing users and roles before seeding.
    /// </summary>
    /// <param name="context">The <see cref="AuthDbContext"/> to seed.</param>
    public static void SeedTestData(AuthDbContext context)
    {
        // Clear existing data
        context.Users.RemoveRange(context.Users);
        context.Roles.RemoveRange(context.Roles);
        context.SaveChanges();

        // Seed roles
        var adminRole = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = "Admin",
            NormalizedName = "ADMIN"
        };

        var userRole = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = "User",
            NormalizedName = "USER"
        };

        context.Roles.AddRange(adminRole, userRole);
        context.SaveChanges();
    }
}
