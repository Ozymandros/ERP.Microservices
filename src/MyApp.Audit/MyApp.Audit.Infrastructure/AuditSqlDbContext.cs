using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MyApp.Audit.Domain;
using MyApp.Shared.Infrastructure.Data;

namespace MyApp.Audit.Infrastructure;

public class AuditSqlDbContext : AuditableDbContext
{
    public AuditSqlDbContext(DbContextOptions<AuditSqlDbContext> options) : base(options)
    {
    }

    public DbSet<EntityChange> EntityChanges => Set<EntityChange>();
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

public class AuditSqlDbContextFactory : IDesignTimeDbContextFactory<AuditSqlDbContext>
{
    public AuditSqlDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuditSqlDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=AuditSqlDb;Trusted_Connection=True;TrustServerCertificate=True;");
        return new AuditSqlDbContext(optionsBuilder.Options);
    }
}
