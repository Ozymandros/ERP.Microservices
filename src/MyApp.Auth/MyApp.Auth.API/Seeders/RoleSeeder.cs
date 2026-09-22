using Microsoft.AspNetCore.Identity;
using MyApp.Auth.Domain.Entities;

namespace MyApp.Auth.API.Seeders;

/// <summary>
/// Seeds the default application roles on application startup.
/// </summary>
public static class RoleSeeder
{
    /// <summary>
    /// Seeds the default roles (Admin, User, Manager), creating any that do not already exist.
    /// </summary>
    /// <param name="roleManager">The ASP.NET Core Identity role manager used to create and query roles.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous seed operation.</returns>
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