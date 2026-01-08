using MyApp.Shared.Domain.DTOs;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Sales.Application.Contracts.DTOs
{
    public record SalesOrderDto(Guid Id) : AuditableGuidDto(Id)
    {
        public DateTime OrderDate { get; init; } = default;
        public string OrderNumber { get; init; } = string.Empty;
        public Guid CustomerId { get; init; } = default;
        public int Status { get; init; } = 0;
        public decimal TotalAmount { get; init; } = 0;
        public CustomerDto? Customer { get; init; }
        public List<SalesOrderLineDto> Lines { get; init; } = new();
    }

    public record SalesOrderLineDto(Guid Id) : AuditableGuidDto(Id)
    {
        public Guid SalesOrderId { get; init; } = default;
        public Guid ProductId { get; init; } = default;

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; init; } = 1;

        [Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be greater than or equal to 0")]
        public decimal UnitPrice { get; init; } = 0;

        public decimal LineTotal { get; init; } = 0;
    }

    public record CustomerDto(Guid Id) : AuditableGuidDto(Id)
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, MinimumLength = 1)]
        public string Name { get; init; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; init; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; init; } = string.Empty;

        [StringLength(500)]
        public string Address { get; init; } = string.Empty;
    }

    public record CreateUpdateSalesOrderDto(
        [Required(ErrorMessage = "OrderNumber is required")]
        [StringLength(64)]
        string OrderNumber,

        [Required(ErrorMessage = "CustomerId is required")]
        Guid CustomerId,

        [Required(ErrorMessage = "OrderDate is required")]
        DateTime OrderDate,

        [Range(0, int.MaxValue, ErrorMessage = "Status must be valid")]
        int Status = 0,

        [Range(0, double.MaxValue, ErrorMessage = "TotalAmount must be greater than or equal to 0")]
        decimal TotalAmount = 0,

        List<CreateUpdateSalesOrderLineDto>? Lines = null
    )
    {
        public List<CreateUpdateSalesOrderLineDto> Lines { get; init; } = Lines ?? new();
    }

    public record CreateUpdateSalesOrderLineDto(
        [Required]
        Guid ProductId,

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        int Quantity,

        [Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be greater than or equal to 0")]
        decimal UnitPrice
    );
}
