using Microsoft.EntityFrameworkCore;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Infrastructure.Data;

namespace MyApp.Inventory.Tests.Helpers;

/// <summary>
/// Helper class for creating in-memory InventoryDbContext instances for testing
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>Creates a new <see cref="InventoryDbContext"/> backed by an isolated in-memory database with a unique name.</summary>
    /// <returns>A freshly created <see cref="InventoryDbContext"/> instance ready for use in tests.</returns>
    public static InventoryDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new InventoryDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    /// <summary>Clears all inventory data from the context and seeds a standard set of test warehouses.</summary>
    /// <param name="context">The <see cref="InventoryDbContext"/> to seed with test data.</param>
    public static void SeedTestData(InventoryDbContext context)
    {
        // Clear existing data
        context.Products.RemoveRange(context.Products);
        context.Warehouses.RemoveRange(context.Warehouses);
        context.InventoryTransactions.RemoveRange(context.InventoryTransactions);
        context.SaveChanges();

        // Seed warehouses
        var warehouse1 = new Warehouse(Guid.NewGuid())
        {
            Name = "Test Warehouse 1",
            Location = "Location 1"
        };

        var warehouse2 = new Warehouse(Guid.NewGuid())
        {
            Name = "Test Warehouse 2",
            Location = "Location 2"
        };

        context.Warehouses.AddRange(warehouse1, warehouse2);
        context.SaveChanges();
    }
}
