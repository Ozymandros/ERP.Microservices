using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyApp.Agentic.Infrastructure.Data;
using MyApp.Shared.Domain.Security;

namespace MyApp.Agentic.Infrastructure.Memory;

public sealed class ProviderBackedMemoryEmbeddingGenerator : IMemoryEmbeddingGenerator
{
    private const string DefaultEmbeddingModel = "text-embedding-3-small";
    private readonly ISecretCryptoService _secretCryptoService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DeterministicTextEmbeddingGenerator _fallbackGenerator;
    private readonly ILogger<ProviderBackedMemoryEmbeddingGenerator> _logger;

    private readonly IServiceProvider _serviceProvider;

    public ProviderBackedMemoryEmbeddingGenerator(
        IServiceProvider serviceProvider,
        ISecretCryptoService secretCryptoService,
        IHttpClientFactory httpClientFactory,
        DeterministicTextEmbeddingGenerator fallbackGenerator,
        ILogger<ProviderBackedMemoryEmbeddingGenerator> logger)
    {
        _serviceProvider = serviceProvider;
        _secretCryptoService = secretCryptoService;
        _httpClientFactory = httpClientFactory;
        _fallbackGenerator = fallbackGenerator;
        _logger = logger;
    }

    public int VectorSize => _fallbackGenerator.VectorSize;

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        // Create a temporary scope when you need to access the DbContext
        using var scope = _serviceProvider.CreateScope();
        using var dbContext = scope.ServiceProvider.GetRequiredService<AgenticSqlDbContext>(); var input = text ?? string.Empty;

        try
        {
            var provider = await dbContext.AIProviders
                .AsNoTracking()
                .Where(p => !string.IsNullOrWhiteSpace(p.EncryptedApiKey))
                .OrderByDescending(p => p.Name == "OpenAI")
                .ThenBy(p => p.Name)
                .FirstOrDefaultAsync(cancellationToken);

            if (provider is null)
            {
                return await _fallbackGenerator.GenerateEmbeddingAsync(input, cancellationToken);
            }

            var apiKey = _secretCryptoService.Decrypt(provider.EncryptedApiKey!);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return await _fallbackGenerator.GenerateEmbeddingAsync(input, cancellationToken);
            }

            var model = string.IsNullOrWhiteSpace(provider.DefaultEmbeddingModelName)
                ? DefaultEmbeddingModel
                : provider.DefaultEmbeddingModelName.Trim();

            var endpoint = BuildEmbeddingsEndpoint(provider.BaseUrl);
            var requestBody = JsonSerializer.Serialize(new
            {
                model,
                input
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var client = _httpClientFactory.CreateClient("MemoryEmbeddingProvider");
            using var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!TryReadFirstEmbedding(document.RootElement, out var values))
            {
                return await _fallbackGenerator.GenerateEmbeddingAsync(input, cancellationToken);
            }

            return NormalizeSize(values, VectorSize);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate provider-backed embedding, using deterministic fallback.");
            return await _fallbackGenerator.GenerateEmbeddingAsync(input, cancellationToken);
        }
    }

    private static string BuildEmbeddingsEndpoint(string baseUrl)
    {
        var normalized = (baseUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = "https://api.openai.com/v1";
        }

        return normalized.EndsWith("/embeddings", StringComparison.OrdinalIgnoreCase)
            ? normalized
            : normalized.TrimEnd('/') + "/embeddings";
    }

    private static bool TryReadFirstEmbedding(JsonElement root, out float[] values)
    {
        values = Array.Empty<float>();

        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array || data.GetArrayLength() == 0)
            return false;

        var first = data[0];
        if (!first.TryGetProperty("embedding", out var embedding) || embedding.ValueKind != JsonValueKind.Array)
            return false;

        var list = new List<float>(embedding.GetArrayLength());
        foreach (var item in embedding.EnumerateArray())
        {
            if (item.TryGetSingle(out var value))
            {
                list.Add(value);
            }
            else if (item.TryGetDouble(out var asDouble))
            {
                list.Add((float)asDouble);
            }
        }

        if (list.Count == 0)
            return false;

        values = list.ToArray();
        return true;
    }

    private static float[] NormalizeSize(float[] source, int targetSize)
    {
        var result = new float[targetSize];
        var copyLength = Math.Min(source.Length, targetSize);
        Array.Copy(source, result, copyLength);

        var magnitude = MathF.Sqrt(result.Sum(v => v * v));
        if (magnitude > 0)
        {
            for (var i = 0; i < result.Length; i++)
            {
                result[i] /= magnitude;
            }
        }

        return result;
    }
}
