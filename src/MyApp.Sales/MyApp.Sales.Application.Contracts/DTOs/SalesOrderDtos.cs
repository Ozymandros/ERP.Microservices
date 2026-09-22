using MyApp.Shared.Domain.DTOs;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Sales.Application.Contracts.DTOs
{
    /// <summary>Represents a sales order with commercial details including customer, status, total amount, and line items.</summary>
    /// <param name="Id">The id.</param>
    public record SalesOrderDto(Guid Id) : AuditableGuidDto(Id)
    {
        /// <summary>Gets the date the order was placed.</summary>
        public DateTime OrderDate { get; init; } = default;
        /// <summary>Gets the unique order number assigned to this sales order.</summary>
        public string OrderNumber { get; init; } = string.Empty;
        /// <summary>Gets the identifier of the customer who placed this order.</summary>
        public Guid CustomerId { get; init; } = default;
        /// <summary>Gets the current status of the order as an integer representation of <see cref="MyApp.Sales.Domain.Entities.SalesOrderStatus"/>.</summary>
        public int Status { get; init; } = 0;
        /// <summary>Gets the total monetary amount for this order.</summary>
        public decimal TotalAmount { get; init; } = 0;
        /// <summary>Gets the associated customer details, if loaded.</summary>
        public CustomerDto? Customer { get; init; }
        /// <summary>Gets the line items belonging to this order.</summary>
        public List<SalesOrderLineDto> Lines { get; init; } = new();

        // Quote tracking
        /// <summary>Gets a value indicating whether this sales order is a quote rather than a confirmed order.</summary>
        public bool IsQuote { get; init; }
        /// <summary>Gets the date and time when this quote expires, or <see langword="null"/> for confirmed orders.</summary>
        public DateTime? QuoteExpiryDate { get; init; }
        /// <summary>Gets the identifier of the fulfillment order created when this quote was confirmed, or <see langword="null"/> if not yet confirmed.</summary>
        public Guid? ConvertedToOrderId { get; init; }
    }

    /// <summary>Represents a single line item within a sales order, containing product, quantity, and pricing information.</summary>
    /// <param name="Id">The id.</param>
    public record SalesOrderLineDto(Guid Id) : AuditableGuidDto(Id)
    {
        /// <summary>Gets the identifier of the parent sales order.</summary>
        public Guid SalesOrderId { get; init; } = default;
        /// <summary>Gets the identifier of the product on this line.</summary>
        public Guid ProductId { get; init; } = default;

        /// <summary>Gets the quantity ordered.</summary>
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; init; } = 1;

        /// <summary>Gets the unit price for the product.</summary>
        [Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be greater than or equal to 0")]
        public decimal UnitPrice { get; init; } = 0;

        /// <summary>Gets the line total computed as quantity multiplied by unit price.</summary>
        public decimal LineTotal { get; init; } = 0;
    }

    /// <summary>Represents a customer in the Sales service, containing contact and address details.</summary>
    /// <param name="Id">The id.</param>
    public record CustomerDto(Guid Id) : AuditableGuidDto(Id)
    {
        /// <summary>Gets the customer's full name.</summary>
        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, MinimumLength = 1)]
        public string Name { get; init; } = string.Empty;

        /// <summary>Gets the customer's email address.</summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; init; } = string.Empty;

        /// <summary>Gets the customer's phone number.</summary>
        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; init; } = string.Empty;

        /// <summary>Gets the customer's mailing address.</summary>
        [StringLength(500)]
        public string Address { get; init; } = string.Empty;
    }

    /// <summary>Represents the data required to create or update a customer record.</summary>
    public record CreateUpdateCustomerDto
    {
        /// <summary>Gets the customer's full name.</summary>
        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, MinimumLength = 1)]
        public string Name { get; init; } = string.Empty;

        /// <summary>Gets the customer's email address.</summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; init; } = string.Empty;

        /// <summary>Gets the customer's phone number.</summary>
        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; init; } = string.Empty;

        /// <summary>Gets the customer's mailing address.</summary>
        [StringLength(500)]
        public string Address { get; init; } = string.Empty;
    }

    /// <summary>Represents the data required to create or update a sales order.</summary>
    /// <param name="CustomerId">The customer Id.</param>
    /// <param name="OrderDate">The order Date.</param>
    /// <param name="ExpectedDeliveryDate">The expected Delivery Date.</param>
    /// <param name="Status">The status.</param>
    /// <param name="TotalAmount">The total Amount.</param>
    /// <param name="Lines">The lines.</param>
    public record CreateUpdateSalesOrderDto(
        [Required(ErrorMessage = "CustomerId is required")]
        Guid CustomerId,

        [Required(ErrorMessage = "OrderDate is required")]
        DateTime OrderDate,

        DateTime? ExpectedDeliveryDate = null,

        [Range(0, int.MaxValue, ErrorMessage = "Status must be a valid value")]
        int Status = 0,

        [Range(0, double.MaxValue, ErrorMessage = "TotalAmount must be greater than or equal to 0")]
        decimal TotalAmount = 0,

        List<CreateUpdateSalesOrderLineDto>? Lines = null
    )
    {
        /// <summary>Gets the line items included in this order.</summary>
        public List<CreateUpdateSalesOrderLineDto> Lines { get; init; } = Lines ?? new();
    }

    /// <summary>Represents the data required to create or update a single line item within a sales order.</summary>
    /// <param name="ProductId">The product Id.</param>
    /// <param name="Quantity">The quantity.</param>
    /// <param name="UnitPrice">The unit Price.</param>
    public record CreateUpdateSalesOrderLineDto(
        [Required]
        Guid ProductId,

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        int Quantity,

        [Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be greater than or equal to 0")]
        decimal UnitPrice
    );

    /// <summary>Represents the data required to create a quote with stock availability validation.</summary>
    /// <param name="OrderNumber">The order Number.</param>
    /// <param name="CustomerId">The customer Id.</param>
    /// <param name="OrderDate">The order Date.</param>
    /// <param name="ValidityDays">The validity Days.</param>
    /// <param name="Lines">The lines.</param>
    public record CreateQuoteDto(
        [Required]
        [StringLength(64)]
        string OrderNumber,

        [Required]
        Guid CustomerId,

        DateTime OrderDate,

        [Range(1, int.MaxValue, ErrorMessage = "Quote validity days must be at least 1")]
        int ValidityDays = 30,

        List<CreateUpdateSalesOrderLineDto>? Lines = null
    )
    {
        /// <summary>Gets the line items included in this quote.</summary>
        public List<CreateUpdateSalesOrderLineDto> Lines { get; init; } = Lines ?? new();
    }

    /// <summary>Represents the data required to confirm a quote and convert it to a confirmed sales order.</summary>
    public record ConfirmQuoteDto
    {
        /// <summary>Gets the identifier of the quote to confirm.</summary>
        [Required]
        public Guid QuoteId { get; init; }

        /// <summary>Gets the identifier of the warehouse from which stock will be fulfilled.</summary>
        [Required]
        public Guid WarehouseId { get; init; }

        /// <summary>Gets the optional shipping address for the confirmed order.</summary>
        [MaxLength(500)]
        public string? ShippingAddress { get; init; }
    }

    /// <summary>Represents the stock availability check result for a single product across warehouses.</summary>
    public record StockAvailabilityCheckDto
    {
        /// <summary>Gets the identifier of the product checked.</summary>
        public Guid ProductId { get; init; }
        /// <summary>Gets the quantity requested in the check.</summary>
        public int RequestedQuantity { get; init; }
        /// <summary>Gets the total quantity available across all warehouses.</summary>
        public int AvailableQuantity { get; init; }
        /// <summary>Gets a value indicating whether the requested quantity can be fulfilled.</summary>
        public bool IsAvailable { get; init; }
        /// <summary>Gets the per-warehouse availability breakdown.</summary>
        public List<WarehouseAvailabilityDto> WarehouseStock { get; init; } = new();
    }

    /// <summary>Represents the available stock quantity for a product at a specific warehouse.</summary>
    public record WarehouseAvailabilityDto
    {
        /// <summary>Gets the identifier of the warehouse.</summary>
        public Guid WarehouseId { get; init; }
        /// <summary>Gets the display name of the warehouse.</summary>
        public string WarehouseName { get; init; } = string.Empty;
        /// <summary>Gets the quantity available at this warehouse.</summary>
        public int AvailableQuantity { get; init; }
    }
}
