namespace MyApp.Shared.Domain.Permissions;

/// <summary>
/// Provides predefined permission constants for all module and action combinations.
/// These constants can be used in authorization policies and permission checks.
/// 
/// Usage:
/// <code>
/// [Authorize(Roles = "Admin")]
/// [Permission(PermissionConstants.Inventory.Read)]
/// public IActionResult GetProducts() => Ok();
/// </code>
/// </summary>
public static class PermissionConstants
{
    /// <summary>Inventory module permissions</summary>
    public static class Inventory
    {
        public const string Read = "Inventory.Read";
        public const string Write = "Inventory.Write";
        public const string Delete = "Inventory.Delete";
        public const string Execute = "Inventory.Execute";
        public const string Export = "Inventory.Export";
        public const string Import = "Inventory.Import";

        /// <summary>All Inventory permissions</summary>
        public static string[] All => new[] { Read, Write, Delete, Execute, Export, Import };
    }

    /// <summary>Purchasing module permissions</summary>
    public static class Purchasing
    {
        public const string Read = "Purchasing.Read";
        public const string Write = "Purchasing.Write";
        public const string Delete = "Purchasing.Delete";
        public const string Execute = "Purchasing.Execute";
        public const string Export = "Purchasing.Export";
        public const string Import = "Purchasing.Import";

        /// <summary>All Purchasing permissions</summary>
        public static string[] All => new[] { Read, Write, Delete, Execute, Export, Import };
    }

    /// <summary>Orders module permissions</summary>
    public static class Orders
    {
        public const string Read = "Orders.Read";
        public const string Write = "Orders.Write";
        public const string Delete = "Orders.Delete";
        public const string Execute = "Orders.Execute";
        public const string Export = "Orders.Export";
        public const string Import = "Orders.Import";

        /// <summary>All Orders permissions</summary>
        public static string[] All => new[] { Read, Write, Delete, Execute, Export, Import };
    }

    /// <summary>Sales module permissions</summary>
    public static class Sales
    {
        public const string Read = "Sales.Read";
        public const string Write = "Sales.Write";
        public const string Delete = "Sales.Delete";
        public const string Execute = "Sales.Execute";
        public const string Export = "Sales.Export";
        public const string Import = "Sales.Import";

        /// <summary>All Sales permissions</summary>
        public static string[] All => new[] { Read, Write, Delete, Execute, Export, Import };
    }

    /// <summary>Billing module permissions</summary>
    public static class Billing
    {
        public const string Read = "Billing.Read";
        public const string Write = "Billing.Write";
        public const string Delete = "Billing.Delete";
        public const string Execute = "Billing.Execute";
        public const string Export = "Billing.Export";
        public const string Import = "Billing.Import";

        /// <summary>All Billing permissions</summary>
        public static string[] All => new[] { Read, Write, Delete, Execute, Export, Import };
    }

    /// <summary>Notification module permissions</summary>
    public static class Notification
    {
        public const string Read = "Notification.Read";
        public const string Write = "Notification.Write";
        public const string Delete = "Notification.Delete";
        public const string Execute = "Notification.Execute";
        public const string Export = "Notification.Export";
        public const string Import = "Notification.Import";

        /// <summary>All Notification permissions</summary>
        public static string[] All => new[] { Read, Write, Delete, Execute, Export, Import };
    }

    /// <summary>Auth module permissions</summary>
    public static class Auth
    {
        public const string Read = "Auth.Read";
        public const string Write = "Auth.Write";
        public const string Delete = "Auth.Delete";
        public const string Execute = "Auth.Execute";
        public const string Export = "Auth.Export";
        public const string Import = "Auth.Import";

        /// <summary>All Auth permissions</summary>
        public static string[] All => new[] { Read, Write, Delete, Execute, Export, Import };
    }

    /// <summary>
    /// Gets all permissions for a specific module.
    /// </summary>
    /// <param name="module">The module to get permissions for</param>
    /// <returns>Array of all permission strings for that module</returns>
    public static string[] GetModulePermissions(ModuleEnum module)
    {
        return module switch
        {
            ModuleEnum.Inventory => Inventory.All,
            ModuleEnum.Purchasing => Purchasing.All,
            ModuleEnum.Orders => Orders.All,
            ModuleEnum.Sales => Sales.All,
            ModuleEnum.Billing => Billing.All,
            ModuleEnum.Notification => Notification.All,
            ModuleEnum.Auth => Auth.All,
            _ => Array.Empty<string>()
        };
    }

    /// <summary>
    /// Gets all permissions across all modules.
    /// </summary>
    /// <returns>Array of all permission strings</returns>
    public static string[] GetAllPermissions()
    {
        var permissions = new List<string>();
        
        foreach (ModuleEnum module in Enum.GetValues(typeof(ModuleEnum)))
        {
            permissions.AddRange(GetModulePermissions(module));
        }

        return permissions.ToArray();
    }

    /// <summary>
    /// Creates a permission string from module and action enums.
    /// </summary>
    /// <param name="module">The module</param>
    /// <param name="action">The action</param>
    /// <returns>Permission string in format "Module.Action"</returns>
    public static string Create(ModuleEnum module, ActionEnum action) 
        => $"{module}.{action}";

    /// <summary>
    /// Gets the module name from a permission string.
    /// </summary>
    /// <param name="permission">Permission string (e.g., "Inventory.Read")</param>
    /// <returns>The module name, or null if invalid</returns>
    public static string? GetModule(string permission)
    {
        var parts = permission?.Split('.') ?? Array.Empty<string>();
        return parts.Length == 2 ? parts[0] : null;
    }

    /// <summary>
    /// Gets the action name from a permission string.
    /// </summary>
    /// <param name="permission">Permission string (e.g., "Inventory.Read")</param>
    /// <returns>The action name, or null if invalid</returns>
    public static string? GetAction(string permission)
    {
        var parts = permission?.Split('.') ?? Array.Empty<string>();
        return parts.Length == 2 ? parts[1] : null;
    }
}
