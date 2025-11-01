namespace MyApp.Shared.Domain.Permissions;

/// <summary>
/// Enumerates all modules in the ERP system.
/// Each module represents a functional area like Inventory, Purchasing, etc.
/// </summary>
public enum ModuleEnum
{
    /// <summary>Inventory module - manages products, warehouses, and stock levels</summary>
    Inventory = 1,

    /// <summary>Purchasing module - manages suppliers and purchase orders</summary>
    Purchasing = 2,

    /// <summary>Orders module - manages customer orders</summary>
    Orders = 3,

    /// <summary>Sales module - manages sales orders and customers</summary>
    Sales = 4,

    /// <summary>Billing module - manages invoices and payments</summary>
    Billing = 5,

    /// <summary>Notification module - manages alerts and communications</summary>
    Notification = 6,

    /// <summary>Auth module - manages users, roles, and authentication</summary>
    Auth = 7
}

/// <summary>
/// Enumerates all permission actions that can be performed in the ERP system.
/// These actions represent CRUD operations and other common actions.
/// </summary>
public enum ActionEnum
{
    /// <summary>Read/View permission - allows viewing/retrieving data</summary>
    Read = 1,

    /// <summary>Create permission - allows creating new data</summary>
    Create = 2,

    /// <summary>Update permission - allows modifying existing data</summary>
    Update = 3,

    /// <summary>Delete permission - allows removing data</summary>
    Delete = 4,

    /// <summary>Execute permission - allows running operations and commands</summary>
    Execute = 5,

    /// <summary>Export permission - allows exporting data</summary>
    Export = 6,

    /// <summary>Import permission - allows importing data</summary>
    Import = 7
}

/// <summary>
/// Represents a permission as a combination of a module and an action.
/// Example: Inventory.Read, Purchasing.Write, Orders.Delete
/// 
/// Usage:
/// <code>
/// var permission = new PermissionEnum 
/// { 
///     Module = ModuleEnum.Inventory, 
///     Action = ActionEnum.Read 
/// };
/// string permissionString = permission.ToString(); // "Inventory.Read"
/// </code>
/// </summary>
public class PermissionEnum
{
    /// <summary>
    /// Gets or sets the module this permission applies to.
    /// </summary>
    public required ModuleEnum Module { get; set; }

    /// <summary>
    /// Gets or sets the action this permission allows.
    /// </summary>
    public required ActionEnum Action { get; set; }

    /// <summary>
    /// Converts the permission to its string representation in the format "Module.Action".
    /// </summary>
    /// <returns>A string like "Inventory.Read"</returns>
    public override string ToString() => $"{Module}.{Action}";

    /// <summary>
    /// Parses a permission string (format: "Module.Action") into a PermissionEnum.
    /// </summary>
    /// <param name="permissionString">The permission string to parse (e.g., "Inventory.Read")</param>
    /// <returns>A PermissionEnum instance, or null if parsing fails</returns>
    /// <example>
    /// var permission = PermissionEnum.Parse("Inventory.Read");
    /// </example>
    public static PermissionEnum? Parse(string permissionString)
    {
        if (string.IsNullOrWhiteSpace(permissionString))
            return null;

        var parts = permissionString.Split('.');
        if (parts.Length != 2)
            return null;

        if (!Enum.TryParse<ModuleEnum>(parts[0], ignoreCase: true, out var module) ||
            !Enum.TryParse<ActionEnum>(parts[1], ignoreCase: true, out var action))
        {
            return null;
        }

        return new PermissionEnum { Module = module, Action = action };
    }

    /// <summary>
    /// Tries to parse a permission string into a PermissionEnum.
    /// </summary>
    /// <param name="permissionString">The permission string to parse</param>
    /// <param name="permission">The parsed permission, or null if parsing fails</param>
    /// <returns>True if parsing succeeded, false otherwise</returns>
    public static bool TryParse(string permissionString, out PermissionEnum? permission)
    {
        permission = Parse(permissionString);
        return permission != null;
    }
}
