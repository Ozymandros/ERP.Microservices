using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MyApp.Inventory.Domain.Entities;
using MyApp.Shared.Infrastructure.Data;

namespace MyApp.Inventory.Infrastructure.Data;

/// <summary>
/// Provides Inventory Db Context functionality.
/// </summary>
public class InventoryDbContext : AuditableDbContext
{
    /// <summary>base.</summary>
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    /// <summary>Gets or sets Products.</summary>
    public DbSet<Product> Products { get; set; }
    /// <summary>Gets or sets Warehouses.</summary>
    public DbSet<Warehouse> Warehouses { get; set; }
    /// <summary>Gets or sets Inventory Transactions.</summary>
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
    /// <summary>Gets or sets Warehouse Stocks.</summary>
    public DbSet<WarehouseStock> WarehouseStocks { get; set; }
    /// <summary>Gets or sets Inventory Reservations.</summary>
    public DbSet<InventoryReservation> InventoryReservations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }
}

/// <summary>
/// Provides Inventory Db Context Factory functionality.
/// </summary>
public class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    /// <summary>Create Db Context.</summary>
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=InventoryDb;Trusted_Connection=True;");

        return new InventoryDbContext(optionsBuilder.Options);
    }
}
