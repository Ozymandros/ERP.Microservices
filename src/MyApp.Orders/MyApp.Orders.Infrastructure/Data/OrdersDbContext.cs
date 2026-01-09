using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MyApp.Orders.Domain.Entities;
using MyApp.Shared.Infrastructure.Data;

namespace MyApp.Orders.Infrastructure.Data;

public class OrdersDbContext : AuditableDbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<ReservedStock> ReservedStocks => Set<ReservedStock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Configurations.OrderConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.OrderLineConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ReservedStockConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}

public class OrdersDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
{
    public OrdersDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrdersDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=OrdersDb;Trusted_Connection=True;");

        return new OrdersDbContext(optionsBuilder.Options);
    }
}
