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
        /// <summary>Permission to read/view Inventory data.</summary>
        public const string Read = "Inventory.Read";
        /// <summary>Permission to write/modify Inventory data.</summary>
        public const string Write = "Inventory.Write";
        /// <summary>Permission to delete Inventory data.</summary>
        public const string Delete = "Inventory.Delete";
        /// <summary>Permission to execute Inventory operations.</summary>
        public const string Execute = "Inventory.Execute";
        /// <summary>Permission to export Inventory data.</summary>
        public const string Export = "Inventory.Export";
        /// <summary>Permission to import Inventory data.</summary>
        public const string Import = "Inventory.Import";

        /// <summary>All Inventory permissions</summary>
        public static string[] All => new[] { Read, Write, Delete, Execute, Export, Import };
    }

    /// <summary>Purchasing module permissions</summary>
    public static class Purchasing
    {
        /// <summary>Permission to read/view Purchasing data.</summary>
        public const string Read = "Purchasing.Read";
        /// <summary>Permission to create new Purchasing records.</summary>
        public const string Create = "Purchasing.Create";
        /// <summary>Permission to update existing Purchasing records.</summary>
        public const string Update = "Purchasing.Update";
        /// <summary>Permission to delete Purchasing records.</summary>
        public const string Delete = "Purchasing.Delete";
        /// <summary>Permission to execute Purchasing operations.</summary>
        public const string Execute = "Purchasing.Execute";
        /// <summary>Permission to export Purchasing data.</summary>
        public const string Export = "Purchasing.Export";
        /// <summary>Permission to import Purchasing data.</summary>
        public const string Import = "Purchasing.Import";

        /// <summary>All Purchasing permissions</summary>
        public static string[] All => new[] { Read, Create, Update, Delete, Execute, Export, Import };
    }
    /// <summary>Orders module permissions</summary>
    public static class Orders
    {
        /// <summary>Permission to read/view Orders data.</summary>
        public const string Read = "Orders.Read";
        /// <summary>Permission to create new Orders records.</summary>
        public const string Create = "Orders.Create";
        /// <summary>Permission to update existing Orders records.</summary>
        public const string Update = "Orders.Update";
        /// <summary>Permission to delete Orders records.</summary>
        public const string Delete = "Orders.Delete";
        /// <summary>Permission to execute Orders operations.</summary>
        public const string Execute = "Orders.Execute";
        /// <summary>Permission to export Orders data.</summary>
        public const string Export = "Orders.Export";
        /// <summary>Permission to import Orders data.</summary>
        public const string Import = "Orders.Import";

        /// <summary>All Orders permissions</summary>
        public static string[] All => new[] { Read, Create, Update, Delete, Execute, Export, Import };
    }

    /// <summary>Sales module permissions</summary>
    public static class Sales
    {
        /// <summary>Permission to read/view Sales data.</summary>
        public const string Read = "Sales.Read";
        /// <summary>Permission to create new Sales records.</summary>
        public const string Create = "Sales.Create";
        /// <summary>Permission to update existing Sales records.</summary>
        public const string Update = "Sales.Update";
        /// <summary>Permission to delete Sales records.</summary>
        public const string Delete = "Sales.Delete";
        /// <summary>Permission to execute Sales operations.</summary>
        public const string Execute = "Sales.Execute";
        /// <summary>Permission to export Sales data.</summary>
        public const string Export = "Sales.Export";
        /// <summary>Permission to import Sales data.</summary>
        public const string Import = "Sales.Import";

        /// <summary>All Sales permissions</summary>
        public static string[] All => new[] { Read, Create, Update, Delete, Execute, Export, Import };
    }

    /// <summary>Billing module permissions</summary>
    public static class Billing
    {
        /// <summary>Permission to read/view Billing data.</summary>
        public const string Read = "Billing.Read";
        /// <summary>Permission to create new Billing records.</summary>
        public const string Create = "Billing.Create";
        /// <summary>Permission to update existing Billing records.</summary>
        public const string Update = "Billing.Update";
        /// <summary>Permission to delete Billing records.</summary>
        public const string Delete = "Billing.Delete";
        /// <summary>Permission to execute Billing operations.</summary>
        public const string Execute = "Billing.Execute";
        /// <summary>Permission to export Billing data.</summary>
        public const string Export = "Billing.Export";
        /// <summary>Permission to import Billing data.</summary>
        public const string Import = "Billing.Import";

        /// <summary>All Billing permissions</summary>
        public static string[] All => new[] { Read, Create, Update, Delete, Execute, Export, Import };
    }

    /// <summary>CRM module permissions</summary>
    public static class CRM
    {
        /// <summary>Permission to read/view CRM data.</summary>
        public const string Read = "CRM.Read";
        /// <summary>Permission to create new CRM records.</summary>
        public const string Create = "CRM.Create";
        /// <summary>Permission to update existing CRM records.</summary>
        public const string Update = "CRM.Update";
        /// <summary>Permission to delete CRM records.</summary>
        public const string Delete = "CRM.Delete";
        /// <summary>Permission to execute CRM operations.</summary>
        public const string Execute = "CRM.Execute";
        /// <summary>Permission to export CRM data.</summary>
        public const string Export = "CRM.Export";
        /// <summary>Permission to import CRM data.</summary>
        public const string Import = "CRM.Import";

        /// <summary>All CRM permissions</summary>
        public static string[] All => new[] { Read, Create, Update, Delete, Execute, Export, Import };
    }

    /// <summary>Notification module permissions</summary>
    public static class Notification
    {
        /// <summary>Permission to read/view Notification data.</summary>
        public const string Read = "Notification.Read";
        /// <summary>Permission to create new Notification records.</summary>
        public const string Create = "Notification.Create";
        /// <summary>Permission to update existing Notification records.</summary>
        public const string Update = "Notification.Update";
        /// <summary>Permission to delete Notification records.</summary>
        public const string Delete = "Notification.Delete";
        /// <summary>Permission to execute Notification operations.</summary>
        public const string Execute = "Notification.Execute";
        /// <summary>Permission to export Notification data.</summary>
        public const string Export = "Notification.Export";
        /// <summary>Permission to import Notification data.</summary>
        public const string Import = "Notification.Import";

        /// <summary>All Notification permissions</summary>
        public static string[] All => new[] { Read, Create, Update, Delete, Execute, Export, Import };
    }

    /// <summary>Auth module permissions</summary>
    public static class Auth
    {
        /// <summary>Permission to read/view Auth data.</summary>
        public const string Read = "Auth.Read";
        /// <summary>Permission to create new Auth records.</summary>
        public const string Create = "Auth.Create";
        /// <summary>Permission to update existing Auth records.</summary>
        public const string Update = "Auth.Update";
        /// <summary>Permission to delete Auth records.</summary>
        public const string Delete = "Auth.Delete";
        /// <summary>Permission to execute Auth operations.</summary>
        public const string Execute = "Auth.Execute";
        /// <summary>Permission to export Auth data.</summary>
        public const string Export = "Auth.Export";
        /// <summary>Permission to import Auth data.</summary>
        public const string Import = "Auth.Import";

        /// <summary>All Auth permissions</summary>
        public static string[] All => new[] { Read, Create, Update, Delete, Execute, Export, Import };
    }

    /// <summary>Users management permissions</summary>
    public static class Users
    {
        /// <summary>Permission to read/view Users data.</summary>
        public const string Read = "Users.Read";
        /// <summary>Permission to create new user accounts.</summary>
        public const string Create = "Users.Create";
        /// <summary>Permission to update existing user accounts.</summary>
        public const string Update = "Users.Update";
        /// <summary>Permission to delete user accounts.</summary>
        public const string Delete = "Users.Delete";
        /// <summary>Permission to execute user management operations.</summary>
        public const string Execute = "Users.Execute";
        /// <summary>Permission to export Users data.</summary>
        public const string Export = "Users.Export";
        /// <summary>Permission to import Users data.</summary>
        public const string Import = "Users.Import";

        /// <summary>All Users permissions</summary>
        public static string[] All => new[] { Read, Create, Update, Delete, Execute, Export, Import };
    }

    /// <summary>Roles management permissions</summary>
    public static class Roles
    {
        /// <summary>Permission to read/view Roles data.</summary>
        public const string Read = "Roles.Read";
        /// <summary>Permission to create new roles.</summary>
        public const string Create = "Roles.Create";
        /// <summary>Permission to update existing roles.</summary>
        public const string Update = "Roles.Update";
        /// <summary>Permission to delete roles.</summary>
        public const string Delete = "Roles.Delete";
        /// <summary>Permission to execute role management operations.</summary>
        public const string Execute = "Roles.Execute";
        /// <summary>Permission to export Roles data.</summary>
        public const string Export = "Roles.Export";
        /// <summary>Permission to import Roles data.</summary>
        public const string Import = "Roles.Import";

        /// <summary>All Roles permissions</summary>
        public static string[] All => new[] { Read, Create, Update, Delete, Execute, Export, Import };
    }

    /// <summary>Permissions management permissions</summary>
    public static class Permissions
    {
        /// <summary>Permission to read/view permission records.</summary>
        public const string Read = "Permissions.Read";
        /// <summary>Permission to create new permission assignments.</summary>
        public const string Create = "Permissions.Create";
        /// <summary>Permission to update existing permission assignments.</summary>
        public const string Update = "Permissions.Update";
        /// <summary>Permission to delete permission assignments.</summary>
        public const string Delete = "Permissions.Delete";
        /// <summary>Permission to execute permission management operations.</summary>
        public const string Execute = "Permissions.Execute";
        /// <summary>Permission to export Permissions data.</summary>
        public const string Export = "Permissions.Export";
        /// <summary>Permission to import Permissions data.</summary>
        public const string Import = "Permissions.Import";

        /// <summary>All Permissions permissions</summary>
        public static string[] All => new[] { Read, Create, Update, Delete, Execute, Export, Import };
    }

    /// <summary>Agentic module permissions - AI agent and bot orchestration</summary>
    public static class Agentic
    {
        /// <summary>Permission to read/view Agentic data and chat history.</summary>
        public const string Read = "Agentic.Read";
        /// <summary>Permission to create new Agentic sessions or agents.</summary>
        public const string Create = "Agentic.Create";
        /// <summary>Permission to update existing Agentic configurations.</summary>
        public const string Update = "Agentic.Update";
        /// <summary>Permission to delete Agentic sessions or agents.</summary>
        public const string Delete = "Agentic.Delete";
        /// <summary>Permission to execute Agentic operations and run agents.</summary>
        public const string Execute = "Agentic.Execute";
        /// <summary>Permission to export Agentic data.</summary>
        public const string Export = "Agentic.Export";
        /// <summary>Permission to import Agentic data.</summary>
        public const string Import = "Agentic.Import";

        /// <summary>All Agentic permissions</summary>
        public static string[] All => new[] { Read, Create, Update, Delete, Execute, Export, Import };
    }

    /// <summary>Audit module permissions</summary>
    public static class Audit
    {
        /// <summary>Permission to read/view audit trail records.</summary>
        public const string Read = "Audit.Read";
        /// <summary>Permission to create new audit entries.</summary>
        public const string Create = "Audit.Create";
        /// <summary>Permission to update audit records.</summary>
        public const string Update = "Audit.Update";
        /// <summary>Permission to delete audit records.</summary>
        public const string Delete = "Audit.Delete";
        /// <summary>Permission to execute audit operations.</summary>
        public const string Execute = "Audit.Execute";
        /// <summary>Permission to export audit data.</summary>
        public const string Export = "Audit.Export";
        /// <summary>Permission to import audit data.</summary>
        public const string Import = "Audit.Import";

        /// <summary>All Audit permissions</summary>
        public static string[] All => new[] { Read, Create, Update, Delete, Execute, Export, Import };
    }

    /// <summary>
    /// Gets the module permissions.
    /// </summary>
    /// <param name="module">The module.</param>
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
            ModuleEnum.CRM => CRM.All,
            ModuleEnum.Auth => Auth.All,
            ModuleEnum.Agentic => Agentic.All,
            ModuleEnum.Audit => Audit.All,
            _ => Array.Empty<string>()
        };
    }

    /// <summary>
    /// Gets all permissions.
    /// </summary>
    /// <returns>Array of all permission strings</returns>
    public static string[] GetAllPermissions()
    {
        var permissions = new List<string>();

        foreach (ModuleEnum module in Enum.GetValues<ModuleEnum>())
        {
            permissions.AddRange(GetModulePermissions(module));
        }

        return permissions.ToArray();
    }

    /// <summary>
    /// Creates a new item.
    /// </summary>
    /// <param name="module">The module.</param>
    /// <param name="action">The action.</param>
    /// <returns>Permission string in format "Module.Action"</returns>
    public static string Create(ModuleEnum module, ActionEnum action)
        => $"{module}.{action}";

    /// <summary>
    /// Gets the module.
    /// </summary>
    /// <param name="permission">The permission.</param>
    /// <returns>The module name, or null if invalid</returns>
    public static string? GetModule(string permission)
    {
        var parts = permission?.Split('.') ?? Array.Empty<string>();
        return parts.Length == 2 ? parts[0] : null;
    }

    /// <summary>
    /// Gets the action.
    /// </summary>
    /// <param name="permission">The permission.</param>
    /// <returns>The action name, or null if invalid</returns>
    public static string? GetAction(string permission)
    {
        var parts = permission?.Split('.') ?? Array.Empty<string>();
        return parts.Length == 2 ? parts[1] : null;
    }
}
