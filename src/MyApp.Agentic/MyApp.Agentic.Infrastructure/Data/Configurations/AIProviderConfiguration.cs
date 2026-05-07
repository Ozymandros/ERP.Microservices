using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Agentic.Domain.AIProviders;

namespace MyApp.Agentic.Infrastructure.Data.Configurations;

public class AIProviderConfiguration : IEntityTypeConfiguration<AIProvider>
{
    public void Configure(EntityTypeBuilder<AIProvider> builder)
    {
        builder.ToTable("AIProviders");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);
        builder.HasIndex(p => p.Name)
            .IsUnique();

        builder.Property(p => p.BaseUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.SecretKeyName)
            .IsRequired()
            .HasMaxLength(200);

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

        builder.HasMany(p => p.Models)
            .WithOne(m => m.Provider)
            .HasForeignKey(m => m.ProviderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}