using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MyApp.Sales.Domain.Entities;
using MyApp.Shared.Infrastructure.Data;

namespace MyApp.Sales.Infrastructure.Data;

/// <summary>
/// Provides Sales Db Context functionality.
/// </summary>
public class SalesDbContext : AuditableDbContext
{
    /// <summary>base.</summary>
    /// <param name="options">The options.</param>
    public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options)
    {
    }

    /// <summary>Set.</summary>
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    /// <summary>Gets the <see cref="DbSet{TEntity}"/> for sales order lines.</summary>
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();
    /// <summary>Gets the <see cref="DbSet{TEntity}"/> for customers.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>Applies entity type configurations for <see cref="SalesOrder"/>, <see cref="SalesOrderLine"/>, and <see cref="Customer"/>.</summary>
    /// <param name="modelBuilder">The model builder used to configure the EF Core model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Configurations.SalesOrderConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.SalesOrderLineConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.CustomerConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}

/// <summary>
/// Provides Sales Db Context Factory functionality.
/// </summary>
public class SalesDbContextFactory : IDesignTimeDbContextFactory<SalesDbContext>
{
    /// <summary>Create Db Context.</summary>
    public SalesDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SalesDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=SalesDb;Trusted_Connection=True;");

        return new SalesDbContext(optionsBuilder.Options);
    }
}


