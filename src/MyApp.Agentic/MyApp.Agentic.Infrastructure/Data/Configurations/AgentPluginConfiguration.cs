using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Agentic.Domain.Agents;

namespace MyApp.Agentic.Infrastructure.Data.Configurations;

public class AgentPluginConfiguration : IEntityTypeConfiguration<AgentPlugin>
{
    public void Configure(EntityTypeBuilder<AgentPlugin> builder)
    {
        builder.ToTable("AgentPlugins");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PluginName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.DaprAppIdEndpoint)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(p => new { p.AgentId, p.PluginName })
            .IsUnique();
    }
}