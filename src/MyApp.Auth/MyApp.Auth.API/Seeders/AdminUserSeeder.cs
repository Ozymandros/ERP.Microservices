using Microsoft.AspNetCore.Identity;
using MyApp.Auth.Domain.Entities;

namespace MyApp.Auth.API.Seeders;

/// <summary>
/// Seeds the default administrator user account on application startup.
/// </summary>
public static class AdminUserSeeder
{
    /// <summary>
    /// Seeds the default admin user, creating it if it does not exist or assigning the Admin role if it is missing.
    /// </summary>
    /// <param name="userManager">The ASP.NET Core Identity user manager used to create and query users.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous seed operation.</returns>
    public static async Task SeedAsync(UserManager<ApplicationUser> userManager)
    {
        var adminEmail = "admin@myapp.local";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            var user = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                IsActive = true
            };

            await userManager.CreateAsync(user, "Admin123!");
            await userManager.AddToRoleAsync(user, "Admin");
        }
        else
        {
            // Ensure password is correct
            //var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
            //await userManager.ResetPasswordAsync(adminUser, token, "Admin123!");

            // Ensure role is assigned
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}