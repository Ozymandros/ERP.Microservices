using Microsoft.EntityFrameworkCore;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Infrastructure.Data;

namespace MyApp.Inventory.Tests.Helpers;

/// <summary>
/// Helper class for creating in-memory InventoryDbContext instances for testing
/// </summary>
public static class TestDbContextFactory
{
    public static InventoryDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new InventoryDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

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
