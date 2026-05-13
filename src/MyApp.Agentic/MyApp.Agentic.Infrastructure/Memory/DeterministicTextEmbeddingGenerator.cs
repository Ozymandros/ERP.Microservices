using System.Security.Cryptography;
using System.Text;

namespace MyApp.Agentic.Infrastructure.Memory;

/// <summary>
/// Produces deterministic normalized embedding vectors from text hashes.
/// Used as a fallback when provider-backed embedding generation is unavailable.
/// </summary>
public sealed class DeterministicTextEmbeddingGenerator : IMemoryEmbeddingGenerator
{
    private const int DefaultVectorSize = 1536;

    /// <inheritdoc />
    public int VectorSize => DefaultVectorSize;

    /// <inheritdoc />
    public Task<float[]> GenerateEmbeddingAsync(
        string text,
        MemoryEmbeddingProviderContext provider,
        CancellationToken cancellationToken = default)
    {
        var input = text ?? string.Empty;
        var vector = new float[VectorSize];

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));

        for (var i = 0; i < VectorSize; i++)
        {
            var b = bytes[i % bytes.Length];
            vector[i] = (b / 255f) * 2f - 1f;
        }

        var magnitude = MathF.Sqrt(vector.Sum(v => v * v));
        if (magnitude > 0)
        {
            for (var i = 0; i < vector.Length; i++)
            {
                vector[i] /= magnitude;
            }
        }

        return Task.FromResult(vector);
    }
}
