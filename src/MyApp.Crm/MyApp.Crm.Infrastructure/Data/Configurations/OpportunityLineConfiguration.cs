using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Crm.Domain.Opportunities;

namespace MyApp.Crm.Infrastructure.Data.Configurations;

public sealed class OpportunityLineConfiguration : IEntityTypeConfiguration<OpportunityLine>
{
    public void Configure(EntityTypeBuilder<OpportunityLine> builder)
    {
        builder.ToTable("OpportunityLines");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OpportunityId).IsRequired();
        builder.HasIndex(x => x.OpportunityId);

        builder.Property(x => x.ProductId);
        builder.Property(x => x.Sku).HasMaxLength(64);

        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.DiscountPercent).HasPrecision(5, 4).IsRequired();
    }
}

