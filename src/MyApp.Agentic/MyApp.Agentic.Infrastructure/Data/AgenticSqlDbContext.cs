using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Data.SqlTypes;
using MyApp.Agentic.Domain.Agents;
using MyApp.Agentic.Domain.AIModels;
using MyApp.Agentic.Domain.AIProviders;
using MyApp.Agentic.Domain.Memory;
using MyApp.Agentic.Domain.Sessions;
using MyApp.Shared.Infrastructure.Data;

namespace MyApp.Agentic.Infrastructure.Data;

/// <summary>Entity Framework Core database context for the Agentic microservice, covering AI providers, models, agents, sessions, and memory.</summary>
public class AgenticSqlDbContext : AuditableDbContext
{
    /// <summary>
    /// Initializes a new instance of the AgenticSqlDbContext class.
    /// </summary>
    /// <param name="options">The options.</param>
    public AgenticSqlDbContext(DbContextOptions<AgenticSqlDbContext> options) : base(options)
    {
    }

    /// <summary>Gets the <see cref="DbSet{TEntity}"/> for AI providers.</summary>
    public DbSet<AIProvider> AIProviders => Set<AIProvider>();
    /// <summary>Gets the <see cref="DbSet{TEntity}"/> for AI models.</summary>
    public DbSet<AIModel> AIModels => Set<AIModel>();
    /// <summary>Gets the <see cref="DbSet{TEntity}"/> for agents.</summary>
    public DbSet<Agent> Agents => Set<Agent>();
    /// <summary>Gets the <see cref="DbSet{TEntity}"/> for agent plugins.</summary>
    public DbSet<AgentPlugin> AgentPlugins => Set<AgentPlugin>();
    /// <summary>Gets the <see cref="DbSet{TEntity}"/> for agent sessions.</summary>
    public DbSet<AgentSession> AgentSessions => Set<AgentSession>();
    /// <summary>Gets the <see cref="DbSet{TEntity}"/> for agent memory entries.</summary>
    public DbSet<AgentMemory> AgentMemories => Set<AgentMemory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Configurations.AIProviderConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.AIModelConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.AgentConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.AgentPluginConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.AgentSessionConfiguration());

        ConfigureAgentMemories(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureAgentMemories(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<AgentMemory>();

        entity.ToTable("AgentMemories");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.SessionId)
            .IsRequired();

        entity.Property(e => e.Role)
            .IsRequired()
            .HasConversion<string>();

        entity.Property(e => e.Content)
            .IsRequired();

        entity.Property(e => e.Metadata)
            .HasColumnType("nvarchar(max)");

        entity.Property(e => e.CreatedAt)
            .IsRequired();

        entity.HasIndex(e => e.SessionId);
        entity.HasIndex(e => e.CreatedAt);

        // Keep domain Embedding as readonly [NotMapped], persist vectors in shadow column.
        entity.Property<SqlVector<float>>("EmbeddingVector")
            .HasColumnType("vector(1536)");
    }
}

/// <summary>Design-time factory that creates a local-SQL-Server-backed <see cref="AgenticSqlDbContext"/> for EF Core tooling (migrations, scaffolding).</summary>
public class AgenticSqlDbContextFactory : IDesignTimeDbContextFactory<AgenticSqlDbContext>
{
    /// <summary>
    /// Creates a db context.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public AgenticSqlDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AgenticSqlDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=AgenticDb;Trusted_Connection=True;TrustServerCertificate=True;");
        return new AgenticSqlDbContext(optionsBuilder.Options);
    }
}
