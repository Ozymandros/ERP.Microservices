using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MyApp.Audit.Domain;
using MyApp.Shared.Infrastructure.Data;

namespace MyApp.Audit.Infrastructure;

/// <summary>
/// Entity Framework Core database context for the Audit service,
/// providing access to entity change and property change records.
/// </summary>
public class AuditSqlDbContext : AuditableDbContext
{
    /// <summary>
    /// Initializes a new instance of the AuditSqlDbContext class.
    /// </summary>
    /// <param name="options">The options.</param>
    public AuditSqlDbContext(DbContextOptions<AuditSqlDbContext> options) : base(options)
    {
    }

    /// <summary>Gets the <see cref="DbSet{TEntity}"/> for entity change audit records.</summary>
    public DbSet<EntityChange> EntityChanges => Set<EntityChange>();
    /// <summary>Gets the <see cref="DbSet{TEntity}"/> for property-level change records.</summary>
    public DbSet<PropertyChange> PropertyChanges => Set<PropertyChange>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureEntityChangess(modelBuilder);

        ConfigurePropertyChangess(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureEntityChangess(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<EntityChange>();

        entity.ToTable("EntityChanges");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.EntityName)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(e => e.EntityId)
            .IsRequired();

        entity.HasIndex(e => e.EntityId);
        entity.HasIndex(e => new { e.EntityName, e.EntityId });

        entity.HasMany(p => p.PropertyChanges)
            .WithOne(a => a.EntityChange)
            .HasForeignKey(a => a.EntityChangeId)
            .OnDelete(DeleteBehavior.Restrict);

        //entity.OwnsOne(e => e.NewValue).ToJson();
        //entity.OwnsOne(e => e.OriginalValue).ToJson();

        modelBuilder.Entity<EntityChange>()
            .Property(x => x.OriginalValue)
            .HasColumnType("json");

        modelBuilder.Entity<EntityChange>()
            .Property(x => x.NewValue)
            .HasColumnType("json");
    }

    private static void ConfigurePropertyChangess(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<PropertyChange>();

        builder.ToTable("PropertyChanges");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.EntityChangeId)
            .IsRequired();

        builder.Property(p => p.PropertyName)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasOne(p => p.EntityChange);
    }
}

/// <summary>
/// Provides <see cref="AuditSqlDbContext"/> creation for design-time EF Core tooling (migrations).
/// </summary>
public class AuditSqlDbContextFactory : IDesignTimeDbContextFactory<AuditSqlDbContext>
{
    /// <summary>
    /// Creates a db context.
    /// used by EF Core design-time tools such as migrations.
    /// </summary>
    /// <returns>A configured <see cref="AuditSqlDbContext"/> instance.</returns>
    public AuditSqlDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuditSqlDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=AuditSqlDb;Trusted_Connection=True;TrustServerCertificate=True;");
        return new AuditSqlDbContext(optionsBuilder.Options);
    }
}
