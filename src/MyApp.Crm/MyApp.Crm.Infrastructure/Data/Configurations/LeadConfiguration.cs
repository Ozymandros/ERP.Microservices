using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Crm.Domain.Leads;
namespace MyApp.Crm.Infrastructure.Data.Configurations;

/// <summary>
/// Provides Lead Configuration functionality.
/// </summary>
public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("Leads");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(100);
        builder.Property(x => x.ContactName).HasMaxLength(200);
        builder.Property(x => x.ContactEmail).HasMaxLength(320);
        builder.Property(x => x.ContactPhone).HasMaxLength(50);
        builder.Property(x => x.OwnerUsername).HasMaxLength(256).IsRequired();

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.HasMany(x => x.Notes)
            .WithOne()
            .HasForeignKey(n => n.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-many via LeadTag join entity is configured in LeadTagConfiguration.

        builder.Navigation(x => x.Notes).AutoInclude(false);
        builder.Navigation(x => x.Tags).AutoInclude(false);
    }
}

