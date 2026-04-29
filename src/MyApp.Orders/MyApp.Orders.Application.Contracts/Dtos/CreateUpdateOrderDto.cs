using MyApp.Orders.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Orders.Application.Contracts.Dtos
{
    /// <summary>
    /// Represents the Create Update Order Dto data record.
    /// </summary>
    public record CreateUpdateOrderDto
    {
        /// <summary>Gets or sets Order Number.</summary>
        [Required]
        public string OrderNumber { get; init; } = string.Empty;

        /// <summary>Gets or sets Order Date.</summary>
        [Required]
        public DateTime OrderDate { get; init; } = DateTime.UtcNow;

        /// <summary>Gets or sets Type.</summary>
        [Required]
        public OrderType Type { get; init; }

        /// <summary>Gets or sets Source Id.</summary>
        public Guid? SourceId { get; init; }
        /// <summary>Gets or sets Target Id.</summary>
        public Guid? TargetId { get; init; }
        /// <summary>Gets or sets External Order Id.</summary>
        public Guid? ExternalOrderId { get; init; }

        /// <summary>Gets or sets Warehouse Id.</summary>
        public Guid? WarehouseId { get; init; }

        /// <summary>Gets or sets Lines.</summary>
        [Required]
        public List<CreateOrderLineDto> Lines { get; init; } = new();
    }
}
