using MyApp.Shared.Domain.Entities;

namespace MyApp.Sales.Domain.Entities
{
    /// <summary>
    /// Provides Customer functionality.
    /// </summary>
    public class Customer(Guid id) : AuditableEntity<Guid>(id)
    {
        /// <summary>Gets or sets Name.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Gets or sets Email.</summary>
        public string Email { get; set; } = string.Empty;
        /// <summary>Gets or sets Phone Number.</summary>
        public string PhoneNumber { get; set; } = string.Empty;
        /// <summary>Gets or sets Address.</summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>Gets or sets Orders.</summary>
        public List<SalesOrder> Orders { get; set; } = new();
    }
}
