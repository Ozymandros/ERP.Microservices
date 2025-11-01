using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using MyApp.Auth.Domain.Entities;
using MyApp.Shared.Domain.Entities;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Threading;

namespace MyApp.Auth.Infrastructure.Data;

public class AuthDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public override DbSet<ApplicationUser> Users { get; set; }
    public override DbSet<ApplicationRole> Roles { get; set; }
    public override DbSet<IdentityUserRole<Guid>> UserRoles { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Permission> Permissions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(b =>
        {
            // Primary key
            b.HasKey(u => u.Id);

            // Indexes for "normalized" username and email, to allow efficient lookups
            b.HasIndex(u => u.NormalizedUserName).HasDatabaseName("UserNameIndex").IsUnique();
            b.HasIndex(u => u.NormalizedEmail).HasDatabaseName("EmailIndex");

            // Maps to the AspNetUsers table
            b.ToTable("AspNetUsers");

            // A concurrency token for use with the optimistic concurrency checking
            b.Property(u => u.ConcurrencyStamp).IsConcurrencyToken();

            // Limit the size of columns to use efficient database types
            b.Property(u => u.UserName).HasMaxLength(256);
            b.Property(u => u.NormalizedUserName).HasMaxLength(256);
            b.Property(u => u.Email).HasMaxLength(256);
            b.Property(u => u.NormalizedEmail).HasMaxLength(256);

            // === Mapeig dels camps d'Auditoria (IAuditableEntity) ===
            // Aquests camps no s'afegeixen autom�ticament per convenci� a IdentityUser
            b.Property(u => u.CreatedAt).HasColumnType("datetime2").IsRequired();
            b.Property(u => u.CreatedBy).HasMaxLength(256).IsRequired();
            b.Property(u => u.UpdatedAt).HasColumnType("datetime2").IsRequired(false); // Nullable
            b.Property(u => u.UpdatedBy).HasMaxLength(256).IsRequired(false); // Nullable

            // The relationships between User and other entity types
            // Note that these relationships are configured with no navigation properties

            // Each User can have many UserClaims
            b.HasMany(e => e.UserClaims).WithOne().HasForeignKey(uc => uc.UserId).IsRequired();

            // Each User can have many UserLogins
            b.HasMany(e => e.UserLogins).WithOne().HasForeignKey(ul => ul.UserId).IsRequired();

            // Each User can have many UserTokens
            b.HasMany(e => e.RefreshTokens).WithOne(e => e.User).HasForeignKey(ut => ut.UserId).IsRequired();

            // Each User can have many UserTokens
            b.HasMany(e => e.UserRoles).WithOne().HasForeignKey(ut => ut.UserId).IsRequired();
        });

        builder.Entity<ApplicationRole>(b =>
        {
            // Primary key
            b.HasKey(r => r.Id);

            // Index for "normalized" role name to allow efficient lookups
            b.HasIndex(r => r.NormalizedName).HasDatabaseName("RoleNameIndex").IsUnique();

            // Maps to the AspNetRoles table
            b.ToTable("AspNetRoles");

            // A concurrency token for use with the optimistic concurrency checking
            b.Property(r => r.ConcurrencyStamp).IsConcurrencyToken();

            // Limit the size of columns to use efficient database types
            b.Property(r => r.Name).HasMaxLength(256);
            b.Property(r => r.NormalizedName).HasMaxLength(256);

            // === Mapeig dels camps d'Auditoria (IAuditableEntity) ===
            // Aquests camps no s'afegeixen autom�ticament per convenci� a IdentityRole
            b.Property(r => r.CreatedAt).HasColumnType("datetime2").IsRequired();
            b.Property(r => r.CreatedBy).HasMaxLength(256).IsRequired();
            b.Property(r => r.UpdatedAt).HasColumnType("datetime2").IsRequired(false); // Nullable
            b.Property(r => r.UpdatedBy).HasMaxLength(256).IsRequired(false); // Nullable

            // The relationships between Role and other entity types
            // Each Role can have many entries in the RoleClaim join table
            b.HasMany(e => e.RoleClaims).WithOne().HasForeignKey(uc => uc.RoleId).IsRequired();

            // Each Role can have many entries in the UserRole join table
            b.HasMany(e => e.UserRoles).WithOne().HasForeignKey(uc => uc.RoleId).IsRequired();

            // Each Role can have many entries in the RolePermission join table
            b.HasMany(e => e.RolePermissions).WithOne(e => e.Role).HasForeignKey(uc => uc.RoleId).IsRequired();
        });

        builder.Entity<ApplicationUserRole>(b =>
        {
            // Primary key
            //b.HasKey(rp => new { rp.RoleId, rp.UserId });
            //b.HasKey(r => r.Id);

            // Maps to the AspNetRoles table
            b.ToTable("AspNetUserRoles");

            //b.HasIndex(rp => new { rp.RoleId, rp.UserId }).IsUnique();

            b.HasOne(rp => rp.Role)
              .WithMany(r => r.UserRoles)
              .HasForeignKey(rp => rp.RoleId)
              .IsRequired();

            b.HasOne(rp => rp.User)
              .WithMany(r => r.UserRoles)
              .HasForeignKey(rp => rp.UserId)
              .IsRequired();
        });

        builder.Entity<RolePermission>(b =>
        {
            b.HasKey(rp => rp.Id);
            b.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();

            b.HasOne(rp => rp.Role)
              .WithMany(r => r.RolePermissions)
              .HasForeignKey(rp => rp.RoleId)
              .IsRequired();
        });


    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditableEntity &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
            if (entry.Entity is IAuditableEntity)
            {
                var entity = (IAuditableEntity)entry.Entity;
                // Resolve current user name from common ambient contexts:
                // 1. Try IHttpContextAccessor from the DbContext service provider (if available)
                // 2. Fall back to Thread.CurrentPrincipal
                // 3. Final fallback to SystemUser
                string currentUser = "SystemUser";
                try
                {
                    var httpContextAccessor = this.GetService<IHttpContextAccessor>();
                    var name = httpContextAccessor?.HttpContext?.User?.Identity?.Name;
                    if (!string.IsNullOrEmpty(name))
                    {
                        currentUser = name;
                    }
                    else if (Thread.CurrentPrincipal?.Identity?.IsAuthenticated == true &&
                             !string.IsNullOrEmpty(Thread.CurrentPrincipal.Identity.Name))
                    {
                        currentUser = Thread.CurrentPrincipal.Identity.Name;
                    }
                }
                catch
                {
                    // If resolving IHttpContextAccessor fails for any reason, keep the default SystemUser
                }
                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                    entity.CreatedBy = currentUser;
                }
                else
                {
                    Entry(entity).Property(p => p.CreatedAt).IsModified = false;
                    Entry(entity).Property(p => p.CreatedBy).IsModified = false;
                }
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = currentUser;
            }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

public class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=AuthDb;Trusted_Connection=True;");

        return new AuthDbContext(optionsBuilder.Options);
    }
}
