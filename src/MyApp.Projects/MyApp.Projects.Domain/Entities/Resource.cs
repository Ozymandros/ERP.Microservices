using MyApp.Shared.Domain.Entities;

namespace MyApp.Projects.Domain.Entities;

/// <summary>Project resource (person or machine). Costs and prices are in local currency.</summary>
public class Resource(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets or sets the resource number (unique).</summary>
    public string ResourceNo { get; set; } = string.Empty;

    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the type.</summary>
    public ResourceType Type { get; set; } = ResourceType.Person;

    /// <summary>Gets or sets the resource group id.</summary>
    public Guid? ResourceGroupId { get; set; }

    /// <summary>Gets or sets the linked Auth user id (persons only).</summary>
    public Guid? UserId { get; set; }

    /// <summary>Gets or sets the base unit of measure (e.g. HOUR).</summary>
    public string BaseUnitOfMeasure { get; set; } = "HOUR";

    /// <summary>Gets or sets the unit cost in LCY.</summary>
    public decimal UnitCost { get; set; }

    /// <summary>Gets or sets the unit price in LCY.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Gets or sets the daily capacity in base unit of measure.</summary>
    public decimal DailyCapacity { get; set; } = 8m;

    /// <summary>Gets or sets whether the resource is blocked.</summary>
    public bool Blocked { get; set; }

    /// <summary>Gets or sets the resource group navigation.</summary>
    public ResourceGroup? ResourceGroup { get; set; }
}

/// <summary>Group of resources (e.g. Consultants, Machinery).</summary>
public class ResourceGroup(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets or sets the code (unique).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;
}
