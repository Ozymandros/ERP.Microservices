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
    public static AuthDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AuthDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

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
