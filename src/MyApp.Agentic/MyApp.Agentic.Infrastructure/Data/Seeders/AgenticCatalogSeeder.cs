using Microsoft.EntityFrameworkCore;
using MyApp.Agentic.Domain.AIModels;
using MyApp.Agentic.Domain.AIProviders;
using MyApp.Agentic.Domain.Agents;

namespace MyApp.Agentic.Infrastructure.Data.Seeders;

public class AgenticCatalogSeeder(AgenticDbContext dbContext)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var providers = BuildProviders();
        foreach (var provider in providers)
        {
            var existing = await dbContext.AIProviders
                .FirstOrDefaultAsync(p => p.Name == provider.Name, cancellationToken);

            if (existing is null)
            {
                await dbContext.AIProviders.AddAsync(provider, cancellationToken);
            }
            else
            {
                existing.Update(provider.Name, provider.BaseUrl, provider.SecretKeyName);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var providerByName = await dbContext.AIProviders.ToDictionaryAsync(p => p.Name, cancellationToken);
        var models = BuildModels(providerByName);
        foreach (var model in models)
        {
            var existing = await dbContext.AIModels
                .FirstOrDefaultAsync(
                    m => m.ProviderId == model.ProviderId && m.TechnicalName == model.TechnicalName,
                    cancellationToken);

            if (existing is null)
            {
                await dbContext.AIModels.AddAsync(model, cancellationToken);
            }
            else
            {
                existing.Update(
                    model.ProviderId,
                    model.CommercialName,
                    model.TechnicalName,
                    model.TokenLimit,
                    model.Capabilities,
                    model.DefaultTemperature,
                    model.DefaultTopK,
                    model.DefaultMaxTokens,
                    model.DefaultEmbeddingDimensions,
                    model.DefaultEnableMemory,
                    model.DefaultEnableRAG,
                    model.DefaultEmbeddingModelName,
                    model.DefaultBotType,
                    model.DefaultSystemPrompt);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<AIProvider> BuildProviders() =>
    [
        new(Guid.NewGuid(), "Google", "https://generativelanguage.googleapis.com/v1beta", "Google__ApiKey"),
        new(Guid.NewGuid(), "OpenAI", "https://api.openai.com/v1", "OpenAI__ApiKey"),
        new(Guid.NewGuid(), "Anthropic", "https://api.anthropic.com/v1", "Anthropic__ApiKey"),
        new(Guid.NewGuid(), "HuggingFace", "https://router.huggingface.co/v1", "HuggingFace__ApiKey")
    ];

    private static IEnumerable<AIModel> BuildModels(IReadOnlyDictionary<string, AIProvider> providers)
    {
        var google = providers["Google"].Id;
        var openAi = providers["OpenAI"].Id;
        var anthropic = providers["Anthropic"].Id;
        var huggingFace = providers["HuggingFace"].Id;

        return
        [
            CreateModel(google, "Gemini 2.5 Pro", "gemini-2.5-pro", 32768, "chat,tool-calling"),
            CreateModel(google, "Gemini 2.5 Flash", "gemini-2.5-flash", 32768, "chat,tool-calling"),

            CreateModel(openAi, "GPT-5", "gpt-5", 32768, "chat,tool-calling,reasoning"),
            CreateModel(openAi, "GPT-5 Mini", "gpt-5-mini", 16384, "chat,tool-calling"),
            CreateModel(openAi, "GPT-4o", "gpt-4o", 16384, "chat,tool-calling,vision"),

            CreateModel(anthropic, "Claude Haiku 4.5", "claude-haiku-4-5", 16384, "chat,tool-calling"),
            CreateModel(anthropic, "Claude Sonnet 4.6", "claude-sonnet-4-6", 32768, "chat,tool-calling,reasoning"),
            CreateModel(anthropic, "Claude Opus 4.7", "claude-opus-4-7", 32768, "chat,tool-calling,reasoning"),

            CreateModel(huggingFace, "MiniMax M2.5", "minimax/m2.5", 16384, "chat,tool-calling"),
            CreateModel(huggingFace, "DeepSeek V4", "deepseek/deepseek-v4", 16384, "chat,tool-calling"),
            CreateModel(huggingFace, "Llama 4", "meta-llama/llama-4", 16384, "chat,tool-calling"),
            CreateModel(huggingFace, "Qwen 3.6", "qwen/qwen3.6", 16384, "chat,tool-calling"),
            CreateModel(huggingFace, "Mistral 7B", "mistralai/mistral-7b", 8192, "chat"),
            CreateModel(huggingFace, "Gemma 4", "google/gemma-4", 8192, "chat"),
            CreateModel(huggingFace, "Kimi", "moonshotai/kimi", 16384, "chat,tool-calling"),
            CreateModel(huggingFace, "MiMo V2.5 Pro", "XiaomiMiMo/MiMo-V2.5-Pro", 1000000, "agent,long-context,code,chat,tool-calling")
        ];
    }

    private static AIModel CreateModel(
        Guid providerId,
        string commercialName,
        string technicalName,
        int tokenLimit,
        string capabilities)
    {
        return new AIModel(
            Guid.NewGuid(),
            providerId,
            commercialName,
            technicalName,
            tokenLimit,
            capabilities,
            defaultTemperature: 0.7,
            defaultTopK: 3,
            defaultMaxTokens: 2048,
            defaultEmbeddingDimensions: 1536,
            defaultEnableMemory: true,
            defaultEnableRAG: true,
            defaultEmbeddingModelName: null,
            defaultBotType: BotType.Chat,
            defaultSystemPrompt: "You are a helpful AI assistant.");
    }
}
