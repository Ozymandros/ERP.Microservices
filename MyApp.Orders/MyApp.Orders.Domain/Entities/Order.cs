using System;
using System.Collections.Generic;

namespace MyApp.Orders.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }

        public List<OrderLine> Lines { get; set; } = new();
    }
}
