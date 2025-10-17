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

            await userManager.CreateAsync(user, "Admin123!"); // 🔐 Canvia la contrasenya en producció
            await userManager.AddToRoleAsync(user, "Administrator");
        }
    }
}