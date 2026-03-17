using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Crm.Domain.Opportunities;

namespace MyApp.Crm.Infrastructure.Data.Configurations;

public class OpportunityConfiguration : IEntityTypeConfiguration<Opportunity>
{
    public void Configure(EntityTypeBuilder<Opportunity> builder)
    {
        builder.ToTable("Opportunities");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.LeadId);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.OwnerUsername).HasMaxLength(256).IsRequired();

        builder.Property(x => x.Stage).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Probability).HasPrecision(5, 4).IsRequired();
        builder.Property(x => x.ExpectedAmount).HasPrecision(18, 2);
        builder.Property(x => x.ConvertedSalesQuoteId);
        builder.Property(x => x.ConvertedSalesQuoteNumber).HasMaxLength(64);

        // DateOnly mapping (EF Core supports DateOnly in modern versions; store as date)
        builder.Property(x => x.ExpectedCloseDate).HasColumnType("date");

        builder.HasMany(x => x.Notes)
            .WithOne()
            .HasForeignKey(n => n.OpportunityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-many via OpportunityTag join entity is configured in OpportunityTagConfiguration.
    }
}

