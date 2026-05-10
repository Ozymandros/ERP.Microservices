namespace MyApp.Agentic.Domain.Memory;

/// <summary>
/// Marks a class as a VectorStore record type.
/// Temporary implementation pending Microsoft.Extensions.VectorData.Abstractions official attributes.
/// TODO: Replace with [VectorStoreRecordAttribute] when Semantic Kernel implementation becomes GA.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal class VectorStoreRecordAttribute : Attribute
{
}

/// <summary>
/// Marks a property as the vector store record key (unique identifier).
/// Temporary implementation pending Microsoft.Extensions.VectorData.Abstractions official attributes.
/// TODO: Replace with [VectorStoreRecordKeyAttribute] when Semantic Kernel implementation becomes GA.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
internal class VectorStoreRecordKeyAttribute : Attribute
{
}

/// <summary>
/// Marks a property as a vector embedding for semantic search.
/// Temporary implementation pending Microsoft.Extensions.VectorData.Abstractions official attributes.
/// TODO: Replace with [VectorStoreRecordVectorAttribute] when Semantic Kernel implementation becomes GA.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
internal class VectorStoreRecordVectorAttribute : Attribute
{
    /// <summary>
    /// Gets the dimensions of the vector embedding.
    /// </summary>
    public int Dimensions { get; }

    /// <summary>
    /// Gets the distance function used for similarity calculations.
    /// </summary>
    public string? DistanceFunction { get; }

    /// <summary>
    /// Initializes a new instance of the VectorStoreRecordVectorAttribute class.
    /// </summary>
    /// <param name="dimensions">The number of dimensions in the vector.</param>
    /// <param name="distanceFunction">The distance function for similarity (e.g., "CosineSimilarity").</param>
    public VectorStoreRecordVectorAttribute(int dimensions, string? distanceFunction = null)
    {
        Dimensions = dimensions;
        DistanceFunction = distanceFunction;
    }
}

/// <summary>
/// Marks a property as a VectorStore record data field (non-vector field).
/// Temporary implementation pending Microsoft.Extensions.VectorData.Abstractions official attributes.
/// TODO: Replace with [VectorStoreRecordDataAttribute] when Semantic Kernel implementation becomes GA.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
internal class VectorStoreRecordDataAttribute : Attribute
{
}
