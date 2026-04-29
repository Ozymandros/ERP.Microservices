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
    public CrmDbContext(DbContextOptions<CrmDbContext> options) : base(options)
    {
    }

    /// <summary>Set.</summary>
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Lead> Leads => Set<Lead>();
    /// <summary>Set.</summary>
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<OpportunityLine> OpportunityLines => Set<OpportunityLine>();
    public DbSet<Activity> Activities => Set<Activity>();
    /// <summary>Set.</summary>
    public DbSet<Note> Notes => Set<Note>();
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

