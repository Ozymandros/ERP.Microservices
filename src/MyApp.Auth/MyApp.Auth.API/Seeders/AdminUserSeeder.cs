using Microsoft.AspNetCore.Identity;
using MyApp.Auth.Domain.Entities;

public static class AdminUserSeeder
{
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
                EmailConfirmed = true
            };

            await userManager.CreateAsync(user, "Admin123!");
            await userManager.AddToRoleAsync(user, "Admin");
        }
        else
        {
            // Ensure password is correct
            var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
            await userManager.ResetPasswordAsync(adminUser, token, "Admin123!");
            
            // Ensure role is assigned
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}