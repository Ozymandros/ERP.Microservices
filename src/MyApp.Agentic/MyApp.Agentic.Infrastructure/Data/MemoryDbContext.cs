using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MyApp.Agentic.Domain.Memory;
using Pgvector;
using Pgvector.EntityFrameworkCore;

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

            // Convert ReadOnlyMemory<float> <-> Pgvector.Vector at EF Core boundary
            entity.Property(e => e.Embedding)
                .HasColumnType("vector(1536)")
                .HasConversion(
                    v => v.HasValue ? new Vector(v.Value.ToArray()) : null,
                    v => v != null ? new ReadOnlyMemory<float>(v.ToArray()) : null);

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
        optionsBuilder.UseNpgsql("Host=localhost;Database=agentic-memory;Username=postgres;Password=Your_strong_(!)Password123", o => o.UseVector());
        return new MemoryDbContext(optionsBuilder.Options);
    }
}
