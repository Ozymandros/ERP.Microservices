using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MyApp.Agentic.Domain.Memory;
using Npgsql;
using NpgsqlTypes;

namespace MyApp.Agentic.Infrastructure.Data;

public class MemoryDbContext : DbContext
{
    public MemoryDbContext(DbContextOptions<MemoryDbContext> options) : base(options)
    {
    }

    public DbSet<AgentMemory> AgentMemories => Set<AgentMemory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<AgentMemory>(entity =>
        {
            entity.ToTable("AgentMemories");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.SessionId)
                .IsRequired();

            entity.Property(e => e.Role)
                .IsRequired()
                .HasConversion<string>();

            entity.Property(e => e.Content)
                .IsRequired();

            entity.Property(e => e.Embedding)
                .HasColumnType("vector(1536)");

            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb");

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.CreatedAt);
        });

        base.OnModelCreating(modelBuilder);
    }
}

public class MemoryDbContextFactory : IDesignTimeDbContextFactory<MemoryDbContext>
{
    public MemoryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MemoryDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=agentic-memory;Username=postgres;Password=Your_strong_(!)Password123");
        return new MemoryDbContext(optionsBuilder.Options);
    }
}