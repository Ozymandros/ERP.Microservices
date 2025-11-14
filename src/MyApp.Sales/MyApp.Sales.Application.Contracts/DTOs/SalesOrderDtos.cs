using MyApp.Shared.Domain.DTOs;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Sales.Application.Contracts.DTOs
{
    public record SalesOrderDto : AuditableGuidDto
    {
        public string OrderNumber { get; init; } = string.Empty;
        public Guid CustomerId { get; init; }
        public DateTime OrderDate { get; init; }
        public int Status { get; init; }
        public decimal TotalAmount { get; init; }
        public CustomerDto? Customer { get; init; }
        public List<SalesOrderLineDto> Lines { get; init; } = new();
    }

    public record SalesOrderLineDto(
        Guid Id,
        Guid SalesOrderId,
        Guid ProductId,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal
    );

    public record CustomerDto(
        Guid Id,
        
        [property: Required(ErrorMessage = "Name is required")]
        [property: StringLength(255, MinimumLength = 1)]
        string Name,

        [property: Required(ErrorMessage = "Email is required")]
        [property: EmailAddress]
        [property: StringLength(255)]
        string Email,

        [property: Phone]
        [property: StringLength(20)]
        string PhoneNumber,

        [property: StringLength(500)]
        string Address
    );

    public record CreateUpdateSalesOrderDto(
        [property: Required(ErrorMessage = "OrderNumber is required")]
        [property: StringLength(64)]
        string OrderNumber,

        [property: Required(ErrorMessage = "CustomerId is required")]
        Guid CustomerId,

        [property: Required(ErrorMessage = "OrderDate is required")]
        DateTime OrderDate,

        [property: Range(0, int.MaxValue, ErrorMessage = "Status must be valid")]
        int Status,

        [property: Range(0, double.MaxValue, ErrorMessage = "TotalAmount must be greater than or equal to 0")]
        decimal TotalAmount,

        List<CreateUpdateSalesOrderLineDto> Lines
    );

    public record CreateUpdateSalesOrderLineDto(
        [property: Required]
        Guid ProductId,

        [property: Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        int Quantity,

        [property: Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be greater than or equal to 0")]
        decimal UnitPrice
    );
}
