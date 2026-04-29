using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Crm.Domain.Accounts;

namespace MyApp.Crm.Infrastructure.Data.Configurations;

/// <summary>
/// Provides Account Configuration functionality.
/// </summary>
public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CustomerId).IsRequired();
        builder.HasIndex(x => x.CustomerId).IsUnique();

        builder.Property(x => x.Name).HasMaxLength(255).IsRequired();
        builder.Property(x => x.TaxId).HasMaxLength(64);
        builder.Property(x => x.BillingAddress).HasMaxLength(500);
        builder.Property(x => x.ShippingAddress).HasMaxLength(500);
        builder.Property(x => x.OwnerUsername).HasMaxLength(256);

        builder.Property(x => x.LastSyncedAt).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasMany(x => x.Contacts)
            .WithOne()
            .HasForeignKey(c => c.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

