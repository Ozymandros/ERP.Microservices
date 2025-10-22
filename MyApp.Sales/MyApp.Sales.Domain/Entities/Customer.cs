using MyApp.Shared.Domain.Entities;

namespace MyApp.Sales.Domain.Entities
{
    public class Customer : AuditableEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public List<SalesOrder> Orders { get; set; } = new();
    }
}
