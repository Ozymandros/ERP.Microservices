using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MyApp.Projects.Domain.Entities;
using MyApp.Shared.Infrastructure.Data;

namespace MyApp.Projects.Infrastructure.Data;

/// <summary>EF Core context for the Projects service (database: projectsdb).</summary>
public class ProjectsDbContext : AuditableDbContext
{
    /// <summary>Initializes a new instance of the <see cref="ProjectsDbContext"/> class.</summary>
    /// <param name="options">The options.</param>
    public ProjectsDbContext(DbContextOptions<ProjectsDbContext> options) : base(options)
    {
    }

    /// <summary>Gets the projects.</summary>
    public DbSet<Project> Projects => Set<Project>();
    /// <summary>Gets the project tasks.</summary>
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
    /// <summary>Gets the planning lines.</summary>
    public DbSet<ProjectPlanningLine> PlanningLines => Set<ProjectPlanningLine>();
    /// <summary>Gets the ledger entries.</summary>
    public DbSet<ProjectLedgerEntry> LedgerEntries => Set<ProjectLedgerEntry>();
    /// <summary>Gets the invoice requests.</summary>
    public DbSet<ProjectInvoiceRequest> InvoiceRequests => Set<ProjectInvoiceRequest>();
    /// <summary>Gets the invoice request lines.</summary>
    public DbSet<ProjectInvoiceRequestLine> InvoiceRequestLines => Set<ProjectInvoiceRequestLine>();
    /// <summary>Gets the consumption orders.</summary>
    public DbSet<ProjectConsumptionOrder> ConsumptionOrders => Set<ProjectConsumptionOrder>();
    /// <summary>Gets the consumption order lines.</summary>
    public DbSet<ProjectConsumptionOrderLine> ConsumptionOrderLines => Set<ProjectConsumptionOrderLine>();
    /// <summary>Gets the resources.</summary>
    public DbSet<Resource> Resources => Set<Resource>();
    /// <summary>Gets the resource groups.</summary>
    public DbSet<ResourceGroup> ResourceGroups => Set<ResourceGroup>();
    /// <summary>Gets the currencies.</summary>
    public DbSet<Currency> Currencies => Set<Currency>();
    /// <summary>Gets the exchange rates.</summary>
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProjectsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

/// <summary>Design-time factory used by <c>dotnet ef</c>.</summary>
public class ProjectsDbContextFactory : IDesignTimeDbContextFactory<ProjectsDbContext>
{
    /// <inheritdoc />
    public ProjectsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProjectsDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=ProjectsDb;Trusted_Connection=True;");

        return new ProjectsDbContext(optionsBuilder.Options);
    }
}
