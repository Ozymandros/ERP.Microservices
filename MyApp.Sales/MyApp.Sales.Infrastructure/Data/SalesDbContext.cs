using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MyApp.Sales.Domain.Entities;

namespace MyApp.Sales.Infrastructure.Data;

public class SalesDbContext : DbContext
{
    public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options)
    {
    }

    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Configurations.SalesOrderConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.SalesOrderLineConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.CustomerConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}

public class SalesDbContextFactory : IDesignTimeDbContextFactory<SalesDbContext>
{
    public SalesDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SalesDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=SalesDb;Trusted_Connection=True;");

        return new SalesDbContext(optionsBuilder.Options);
    }
}
