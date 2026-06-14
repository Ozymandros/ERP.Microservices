using Microsoft.AspNetCore.Identity;
using MyApp.Auth.Domain.Entities;

namespace MyApp.Auth.API.Seeders;

/// <summary>
/// Provides Role Seeder functionality.
/// </summary>
public static class RoleSeeder
{
    /// <summary>
    /// Seed Roles. Creates default roles if they don't exist.
    /// </summary>
    /// <param name="roleManager"></param>
    /// <returns></returns>
    public static async Task SeedAsync(RoleManager<ApplicationRole> roleManager)
    {
        var roles = new[] { "Admin", "User", "Manager" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole(role));
            }
        }
    }
}