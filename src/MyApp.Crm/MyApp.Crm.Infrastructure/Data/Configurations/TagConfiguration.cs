using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Crm.Domain.Tags;

namespace MyApp.Crm.Infrastructure.Data.Configurations;

/// <summary>
/// Provides Tag Configuration functionality.
/// </summary>
public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    /// <summary>Configure.</summary>
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

