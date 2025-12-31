using MyApp.Shared.Domain.DTOs;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Sales.Application.Contracts.DTOs
{
    public record SalesOrderDto(
        Guid Id,
        DateTime CreatedAt = default,
        string CreatedBy = "",
        DateTime? UpdatedAt = null,
        string? UpdatedBy = null,
        string OrderNumber = "",
        Guid CustomerId = default,
        DateTime OrderDate = default,
        int Status = 0,
        decimal TotalAmount = 0,
        CustomerDto? Customer = null,
        List<SalesOrderLineDto>? Lines = null
    ) : AuditableGuidDto(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
    {
        public List<SalesOrderLineDto> Lines { get; set; } = Lines ?? new();
    }

    public record SalesOrderLineDto(
        Guid Id,
        DateTime CreatedAt = default,
        string CreatedBy = "",
        DateTime? UpdatedAt = null,
        string? UpdatedBy = null,
        Guid SalesOrderId = default,
        Guid ProductId = default,
        [property: Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        int Quantity = 1,
        [property: Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be greater than or equal to 0")]
        decimal UnitPrice = 0,
        decimal LineTotal = 0
    ) : AuditableGuidDto(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy);

    public record CustomerDto(
        Guid Id,
        DateTime CreatedAt = default,
        string CreatedBy = "",
        DateTime? UpdatedAt = null,
        string? UpdatedBy = null,

        [property: Required(ErrorMessage = "Name is required")]
        [property: StringLength(255, MinimumLength = 1)]
        string Name = "",

        [property: Required(ErrorMessage = "Email is required")]
        [property: EmailAddress]
        [property: StringLength(255)]
        string Email = "",

        [property: Phone]
        [property: StringLength(20)]
        string PhoneNumber = "",

        [property: StringLength(500)]
        string Address = ""
    ) : AuditableGuidDto(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy);

    public record CreateUpdateSalesOrderDto(
        [property: Required(ErrorMessage = "OrderNumber is required")]
        [property: StringLength(64)]
        string OrderNumber,

        [property: Required(ErrorMessage = "CustomerId is required")]
        Guid CustomerId,

        [property: Required(ErrorMessage = "OrderDate is required")]
        DateTime OrderDate,

        [property: Range(0, int.MaxValue, ErrorMessage = "Status must be valid")]
        int Status = 0,

        [property: Range(0, double.MaxValue, ErrorMessage = "TotalAmount must be greater than or equal to 0")]
        decimal TotalAmount = 0,

        List<CreateUpdateSalesOrderLineDto>? Lines = null
    )
    {
        public List<CreateUpdateSalesOrderLineDto> Lines { get; set; } = Lines ?? new();
    }

    public record CreateUpdateSalesOrderLineDto(
        [property: Required]
        Guid ProductId,

        [property: Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        int Quantity,

        [property: Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be greater than or equal to 0")]
        decimal UnitPrice
    );
}
