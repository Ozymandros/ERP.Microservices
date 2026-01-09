using MyApp.Shared.Domain.Entities;

namespace MyApp.Orders.Domain.Entities
{
    public class ReservedStock(Guid id) : AuditableEntity<Guid>(id)
    {
        public Guid ProductId { get; set; }
        public Guid WarehouseId { get; set; }
        public Guid OrderId { get; set; }
        public Guid? OrderLineId { get; set; }
        public int Quantity { get; set; }
        public DateTime ReservedUntil { get; set; }
        public ReservationStatus Status { get; set; }
    }
}
