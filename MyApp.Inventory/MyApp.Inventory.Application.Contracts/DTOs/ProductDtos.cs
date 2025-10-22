using System.ComponentModel.DataAnnotations;

namespace MyApp.Inventory.Application.Contracts.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }

    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int QuantityInStock { get; set; }
    public int ReorderLevel { get; set; }
}

public class CreateUpdateProductDto
{
    [Required(ErrorMessage = "SKU is required")]
    [StringLength(64, MinimumLength = 1)]
    public string SKU { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required")]
    [StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "UnitPrice must be greater than or equal to 0")]
    public decimal UnitPrice { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "QuantityInStock must be greater than or equal to 0")]
    public int QuantityInStock { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "ReorderLevel must be greater than or equal to 0")]
    public int ReorderLevel { get; set; }
}
