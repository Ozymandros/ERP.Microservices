using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MyApp.Orders.Domain.Entities;
using MyApp.Shared.Infrastructure.Data;

namespace MyApp.Orders.Infrastructure.Data;

/// <summary>DbContext for Orders domain.</summary>
public class OrdersDbContext : AuditableDbContext
{
    /// <summary>Initializes a new instance of the OrdersDbContext class.</summary>
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options)
    {
    }

    /// <summary>Gets the Orders DbSet.</summary>
    public DbSet<Order> Orders => Set<Order>();
    /// <summary>Gets the OrderLines DbSet.</summary>
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    /// <summary>Gets the ReservedStocks DbSet.</summary>
    public DbSet<ReservedStock> ReservedStocks => Set<ReservedStock>();

    /// <summary>Configures the model for the context.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Configurations.OrderConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.OrderLineConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ReservedStockConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}

/// <summary>Factory for creating OrdersDbContext at design time.</summary>
public class OrdersDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
{
    /// <summary>Creates an OrdersDbContext instance for design-time operations.</summary>
    public OrdersDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrdersDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=OrdersDb;Trusted_Connection=True;");

        return new OrdersDbContext(optionsBuilder.Options);
    }
}
