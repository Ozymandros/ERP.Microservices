using Microsoft.EntityFrameworkCore;
using MyApp.Auth.Domain.Entities;
using MyApp.Shared.Domain.Permissions;

namespace MyApp.Auth.Infrastructure.Data.Seeders;

/// <summary>
/// Seeds the database with default permissions for all modules and actions.
/// This seeder ensures that the Permission table is populated with all
/// permission combinations defined in PermissionConstants.
/// </summary>
public static class PermissionSeeder
{
    /// <summary>
    /// Seeds all default permissions to the database.
    /// Should be called during application startup or migration execution.
    /// </summary>
    /// <param name="context">The Auth database context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public static async Task SeedPermissionsAsync(AuthDbContext context)
    {
        // Check if permissions already exist to avoid duplicate seeding
        if (await context.Permissions.AnyAsync())
        {
            return;
        }

        var permissions = GetDefaultPermissions();
        await context.Permissions.AddRangeAsync(permissions);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets all default permissions to be seeded.
    /// </summary>
    /// <returns>Collection of Permission entities</returns>
    private static IEnumerable<Permission> GetDefaultPermissions()
    {
        var permissions = new List<Permission>();

        // Add all permissions from PermissionConstants
        AddModulePermissions(permissions, "Inventory", PermissionConstants.Inventory.All);
        AddModulePermissions(permissions, "Purchasing", PermissionConstants.Purchasing.All);
        AddModulePermissions(permissions, "Orders", PermissionConstants.Orders.All);
        AddModulePermissions(permissions, "Sales", PermissionConstants.Sales.All);
        AddModulePermissions(permissions, "Billing", PermissionConstants.Billing.All);
        AddModulePermissions(permissions, "Notification", PermissionConstants.Notification.All);
        AddModulePermissions(permissions, "Auth", PermissionConstants.Auth.All);
        AddModulePermissions(permissions, "Users", PermissionConstants.Users.All);
        AddModulePermissions(permissions, "Roles", PermissionConstants.Roles.All);
        AddModulePermissions(permissions, "Permissions", PermissionConstants.Permissions.All);

        return permissions;
    }

    /// <summary>
    /// Adds permissions for a specific module to the collection.
    /// </summary>
    /// <param name="permissions">Collection to add permissions to</param>
    /// <param name="module">Module name (e.g., "Inventory")</param>
    /// <param name="permissionStrings">Array of permission strings (e.g., ["Inventory.Read", "Inventory.Create"])</param>
    private static void AddModulePermissions(List<Permission> permissions, string module, string[] permissionStrings)
    {
        foreach (var permissionString in permissionStrings)
        {
            var action = PermissionConstants.GetAction(permissionString);
            if (action != null)
            {
                permissions.Add(new Permission(Guid.NewGuid())
                {
                    Module = module,
                    Action = action,
                    Description = $"{module}.{action} - {GetActionDescription(action)}"
                });
            }
        }
    }

    /// <summary>
    /// Gets a user-friendly description for a permission action.
    /// </summary>
    /// <param name="action">The action name</param>
    /// <returns>A descriptive string</returns>
    private static string GetActionDescription(string action) =>
        action switch
        {
            "Read" => "View and retrieve data",
            "Create" => "Create new data",
            "Update" => "Modify existing data",
            "Delete" => "Remove data",
            "Execute" => "Run operations and commands",
            "Export" => "Export data to external formats",
            "Import" => "Import data from external sources",
            _ => "Unknown action"
        };
}
