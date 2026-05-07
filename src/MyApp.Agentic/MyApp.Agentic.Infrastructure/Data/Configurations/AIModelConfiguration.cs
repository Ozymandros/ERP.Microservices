using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Agentic.Domain.AIModels;

namespace MyApp.Agentic.Infrastructure.Data.Configurations;

public class AIModelConfiguration : IEntityTypeConfiguration<AIModel>
{
    public void Configure(EntityTypeBuilder<AIModel> builder)
    {
        builder.ToTable("AIModels");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.TechnicalName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.CommercialName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.TokenLimit)
            .IsRequired();

        builder.Property(p => p.Capabilities)
            .HasMaxLength(1000);

        builder.Property(p => p.DefaultTemperature)
            .IsRequired()
            .HasPrecision(3, 2)
            .HasDefaultValue(0.7);

        builder.Property(p => p.DefaultTopK)
            .IsRequired()
            .HasDefaultValue(3);

        builder.Property(p => p.DefaultMaxTokens)
            .IsRequired()
            .HasDefaultValue(2048);

        builder.Property(p => p.DefaultEmbeddingDimensions)
            .IsRequired()
            .HasDefaultValue(1536);

        builder.Property(p => p.DefaultEnableMemory)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.DefaultEnableRAG)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.DefaultEmbeddingModelName)
            .HasMaxLength(200);

        builder.Property(p => p.DefaultBotType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(Domain.Agents.BotType.Chat);

        builder.Property(p => p.DefaultSystemPrompt)
            .HasMaxLength(8000);

        builder.HasIndex(p => new { p.ProviderId, p.TechnicalName })
            .IsUnique();
        builder.HasIndex(p => new { p.ProviderId, p.CommercialName })
            .IsUnique();

        builder.HasMany(p => p.Agents)
            .WithOne(a => a.Model)
            .HasForeignKey(a => a.ModelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}