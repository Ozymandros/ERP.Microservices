using Microsoft.EntityFrameworkCore;
using MyApp.Sales.Domain.Entities;
using MyApp.Sales.Infrastructure.Data;

namespace MyApp.Sales.Tests.Helpers;

/// <summary>
/// Helper class for creating in-memory SalesDbContext instances for testing
/// </summary>
public static class TestDbContextFactory
{
    public static SalesDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new SalesDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static void SeedTestData(SalesDbContext context)
    {
        // Clear existing data
        context.SalesOrders.RemoveRange(context.SalesOrders);
        context.SalesOrderLines.RemoveRange(context.SalesOrderLines);
        context.Customers.RemoveRange(context.Customers);
        context.SaveChanges();

        // Seed customers
        var customer1 = new Customer(Guid.NewGuid())
        {
            Name = "Test Customer 1",
            Email = "customer1@test.com",
            PhoneNumber = "123-456-7890"
        };

        var customer2 = new Customer(Guid.NewGuid())
        {
            Name = "Test Customer 2",
            Email = "customer2@test.com",
            PhoneNumber = "098-765-4321"
        };

        context.Customers.AddRange(customer1, customer2);
        context.SaveChanges();
    }
}
