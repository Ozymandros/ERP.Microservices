using MyApp.Shared.Domain.DTOs;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Sales.Application.Contracts.DTOs
{
    public class SalesOrderDto : AuditableGuidDto
    {
        public string OrderNumber { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public int Status { get; set; }
        public decimal TotalAmount { get; set; }
        public CustomerDto? Customer { get; set; }
        public List<SalesOrderLineDto> Lines { get; set; } = new();
    }

    public class SalesOrderLineDto
    {
        public Guid Id { get; set; }
        public Guid SalesOrderId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class CustomerDto
    {
        public Guid Id { get; set; }
        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(500)]
        public string Address { get; set; } = string.Empty;
    }

    public class CreateUpdateSalesOrderDto
    {
        [Required(ErrorMessage = "OrderNumber is required")]
        [StringLength(64)]
        public string OrderNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "CustomerId is required")]
        public Guid CustomerId { get; set; }

        [Required(ErrorMessage = "OrderDate is required")]
        public DateTime OrderDate { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Status must be valid")]
        public int Status { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "TotalAmount must be greater than or equal to 0")]
        public decimal TotalAmount { get; set; }

        public List<CreateUpdateSalesOrderLineDto> Lines { get; set; } = new();
    }

    public class CreateUpdateSalesOrderLineDto
    {
        [Required]
        public Guid ProductId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be greater than or equal to 0")]
        public decimal UnitPrice { get; set; }
    }
}
