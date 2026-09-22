using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MyApp.Crm.Domain.Accounts;
using MyApp.Crm.Domain.Activities;
using MyApp.Crm.Domain.Leads;
using MyApp.Crm.Domain.Notes;
using MyApp.Crm.Domain.Opportunities;
using MyApp.Crm.Domain.Tags;
using MyApp.Shared.Infrastructure.Data;

namespace MyApp.Crm.Infrastructure.Data;

/// <summary>
/// Provides Crm Db Context functionality.
/// </summary>
public class CrmDbContext : AuditableDbContext
{
    /// <summary>base.</summary>
    /// <param name="options">The options.</param>
    public CrmDbContext(DbContextOptions<CrmDbContext> options) : base(options)
    {
    }

    /// <summary>Gets the DbSet for Account entities.</summary>
    public DbSet<Account> Accounts => Set<Account>();
    /// <summary>Gets the DbSet for Contact entities.</summary>
    public DbSet<Contact> Contacts => Set<Contact>();
    /// <summary>Gets the DbSet for Lead entities.</summary>
    public DbSet<Lead> Leads => Set<Lead>();
    /// <summary>Gets the DbSet for Opportunity entities.</summary>
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    /// <summary>Gets the DbSet for OpportunityLine entities.</summary>
    public DbSet<OpportunityLine> OpportunityLines => Set<OpportunityLine>();
    /// <summary>Gets the DbSet for Activity entities.</summary>
    public DbSet<Activity> Activities => Set<Activity>();
    /// <summary>Gets the DbSet for Note entities.</summary>
    public DbSet<Note> Notes => Set<Note>();
    /// <summary>Gets the DbSet for Tag entities.</summary>
    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Configurations.AccountConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ContactConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.LeadConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.LeadTagConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.OpportunityConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.OpportunityLineConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.OpportunityTagConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ActivityConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.NoteConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.TagConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}

/// <summary>
/// Provides Crm Db Context Factory functionality.
/// </summary>
public class CrmDbContextFactory : IDesignTimeDbContextFactory<CrmDbContext>
{
    /// <summary>Create Db Context.</summary>
    public CrmDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CrmDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=CrmDb;Trusted_Connection=True;");
        return new CrmDbContext(optionsBuilder.Options);
    }
}

