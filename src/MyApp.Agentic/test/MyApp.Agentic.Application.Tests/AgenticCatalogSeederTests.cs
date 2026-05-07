using Microsoft.EntityFrameworkCore;
using MyApp.Agentic.Infrastructure.Data;
using MyApp.Agentic.Infrastructure.Data.Seeders;

namespace MyApp.Agentic.Application.Tests;

public class AgenticCatalogSeederTests
{
    [Fact]
    public async Task SeedAsync_IsIdempotent_ForProvidersAndModels()
    {
        var options = new DbContextOptionsBuilder<AgenticDbContext>()
            .UseInMemoryDatabase($"agentic-seeder-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new AgenticDbContext(options);
        var sut = new AgenticCatalogSeeder(dbContext);

        await sut.SeedAsync();
        var providerCountFirst = await dbContext.AIProviders.CountAsync();
        var modelCountFirst = await dbContext.AIModels.CountAsync();

        await sut.SeedAsync();
        var providerCountSecond = await dbContext.AIProviders.CountAsync();
        var modelCountSecond = await dbContext.AIModels.CountAsync();

        Assert.Equal(providerCountFirst, providerCountSecond);
        Assert.Equal(modelCountFirst, modelCountSecond);
        Assert.True(providerCountSecond >= 4);
        Assert.True(modelCountSecond >= 16);
    }

    [Fact]
    public async Task SeedAsync_IncludesMimoV25ProUnderHuggingFace()
    {
        var options = new DbContextOptionsBuilder<AgenticDbContext>()
            .UseInMemoryDatabase($"agentic-seeder-mimo-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new AgenticDbContext(options);
        var sut = new AgenticCatalogSeeder(dbContext);

        await sut.SeedAsync();

        var model = await dbContext.AIModels
            .Include(m => m.Provider)
            .FirstOrDefaultAsync(m => m.TechnicalName == "XiaomiMiMo/MiMo-V2.5-Pro");

        Assert.NotNull(model);
        Assert.Equal("MiMo V2.5 Pro", model!.CommercialName);
        Assert.Equal("HuggingFace", model.Provider?.Name);
        Assert.True(model.TokenLimit >= 1000000);
    }

    [Fact]
    public async Task SeedAsync_SetsProviderDefaultConfiguration()
    {
        var options = new DbContextOptionsBuilder<AgenticDbContext>()
            .UseInMemoryDatabase($"agentic-seeder-provider-defaults-{Guid.NewGuid()}")
            .Options;

        await using var dbContext = new AgenticDbContext(options);
        var sut = new AgenticCatalogSeeder(dbContext);

        await sut.SeedAsync();

        var provider = await dbContext.AIProviders.FirstOrDefaultAsync(p => p.Name == "OpenAI");
        Assert.NotNull(provider);
        Assert.Equal(0.7, provider!.DefaultTemperature);
        Assert.Equal(3, provider.DefaultTopK);
        Assert.Equal(2048, provider.DefaultMaxTokens);
        Assert.Equal(1536, provider.DefaultEmbeddingDimensions);
        Assert.True(provider.DefaultEnableMemory);
        Assert.True(provider.DefaultEnableRAG);
        Assert.Equal("You are a helpful AI assistant.", provider.DefaultSystemPrompt);
    }
}
