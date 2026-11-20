using Microsoft.EntityFrameworkCore;
using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Infrastructure.Data;

namespace MyApp.Orders.Tests.Helpers;

/// <summary>
/// Helper class for creating in-memory OrdersDbContext instances for testing
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>Creates a new in-memory <see cref="OrdersDbContext"/> instance with a unique database name.</summary>
    /// <returns>A freshly created and ensured <see cref="OrdersDbContext"/> backed by an in-memory database.</returns>
    public static OrdersDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new OrdersDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    /// <summary>Seeds the provided context with baseline test data, clearing any existing orders and lines first.</summary>
    /// <param name="context">The database context to seed.</param>
    public static void SeedTestData(OrdersDbContext context)
    {
        // Clear existing data
        context.Orders.RemoveRange(context.Orders);
        context.OrderLines.RemoveRange(context.OrderLines);
        context.SaveChanges();

        // Seed test data if needed
        // Add sample orders or order lines here if required for common test scenarios
    }
}
