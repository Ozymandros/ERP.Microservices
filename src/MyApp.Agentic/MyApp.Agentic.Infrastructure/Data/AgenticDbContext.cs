using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MyApp.Agentic.Domain.AIProviders;
using MyApp.Agentic.Domain.AIModels;
using MyApp.Agentic.Domain.Agents;
using MyApp.Agentic.Domain.Sessions;
using MyApp.Shared.Infrastructure.Data;

namespace MyApp.Agentic.Infrastructure.Data;

public class AgenticDbContext : AuditableDbContext
{
    public AgenticDbContext(DbContextOptions<AgenticDbContext> options) : base(options)
    {
    }

    public DbSet<AIProvider> AIProviders => Set<AIProvider>();
    public DbSet<AIModel> AIModels => Set<AIModel>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<AgentPlugin> AgentPlugins => Set<AgentPlugin>();
    public DbSet<AgentSession> AgentSessions => Set<AgentSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Configurations.AIProviderConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.AIModelConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.AgentConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.AgentPluginConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.AgentSessionConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}

public class AgenticDbContextFactory : IDesignTimeDbContextFactory<AgenticDbContext>
{
    public AgenticDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AgenticDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=AgenticDb;Trusted_Connection=True;");
        return new AgenticDbContext(optionsBuilder.Options);
    }
}