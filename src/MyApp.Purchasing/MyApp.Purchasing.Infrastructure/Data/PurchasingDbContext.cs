using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Shared.Infrastructure.Data;

namespace MyApp.Purchasing.Infrastructure.Data;

/// <summary>
/// Provides Purchasing Db Context functionality.
/// </summary>
public class PurchasingDbContext : AuditableDbContext
{
    /// <summary>base.</summary>
    public PurchasingDbContext(DbContextOptions<PurchasingDbContext> options) : base(options)
    {
    }

    /// <summary>Gets or sets Suppliers.</summary>
    public DbSet<Supplier> Suppliers { get; set; }
    /// <summary>Gets or sets Purchase Orders.</summary>
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    /// <summary>Gets or sets Purchase Order Lines.</summary>
    public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PurchasingDbContext).Assembly);
    }
}

/// <summary>
/// Provides Purchasing Db Context Factory functionality.
/// </summary>
public class PurchasingDbContextFactory : IDesignTimeDbContextFactory<PurchasingDbContext>
{
    /// <summary>Create Db Context.</summary>
    public PurchasingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PurchasingDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=PurchasingDb;Trusted_Connection=True;");

        return new PurchasingDbContext(optionsBuilder.Options);
    }
}
