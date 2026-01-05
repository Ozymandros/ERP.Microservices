using Microsoft.EntityFrameworkCore;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Infrastructure.Data;

namespace MyApp.Purchasing.Tests.Helpers;

/// <summary>
/// Helper class for creating in-memory PurchasingDbContext instances for testing
/// </summary>
public static class TestDbContextFactory
{
    public static PurchasingDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<PurchasingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new PurchasingDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static void SeedTestData(PurchasingDbContext context)
    {
        // Clear existing data
        context.Suppliers.RemoveRange(context.Suppliers);
        context.PurchaseOrders.RemoveRange(context.PurchaseOrders);
        context.PurchaseOrderLines.RemoveRange(context.PurchaseOrderLines);
        context.SaveChanges();

        // Seed suppliers
        var supplier1 = new Supplier(Guid.NewGuid())
        {
            Name = "Test Supplier 1",
            Email = "supplier1@test.com",
            PhoneNumber = "123-456-7890"
        };

        var supplier2 = new Supplier(Guid.NewGuid())
        {
            Name = "Test Supplier 2",
            Email = "supplier2@test.com",
            PhoneNumber = "098-765-4321"
        };

        context.Suppliers.AddRange(supplier1, supplier2);
        context.SaveChanges();
    }
}
