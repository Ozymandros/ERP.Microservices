using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;

namespace MyApp.Shared.Infrastructure.OpenApi;

/// <summary>
/// Schema transformer that intercepts DateTime schema generation before JsonSchemaExporter tries to serialize default values.
/// This prevents JsonException when OpenAPI schema generation tries to serialize default(DateTime) values.
/// </summary>
public sealed class DateTimeSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (schema == null) return Task.CompletedTask;

        // Fix DateTime schema at the type level before JsonSchemaExporter tries to serialize default values
        // This intercepts the schema generation BEFORE the serializer attempts to serialize default(DateTime)
        if (context.JsonTypeInfo.Type == typeof(DateTime) || context.JsonTypeInfo.Type == typeof(DateTime?))
        {
            schema.Type = "string";
            schema.Format = "date-time";
            schema.Default = null; // Prevents the engine from trying to serialize a default(DateTime)
            schema.Example = null;
            return Task.CompletedTask;
        }

        // Fix DateTime schema by format detection (fallback)
        if (schema.Type == "string" && schema.Format == "date-time")
        {
            schema.Default = null;
            schema.Example = null;
        }

        // Recursively fix nested schemas
        if (schema.Properties != null)
        {
            foreach (var property in schema.Properties.Values)
            {
                if (property is OpenApiSchema propertySchema)
                {
                    TransformAsync(propertySchema, context, cancellationToken);
                }
            }
        }

        if (schema.Items is OpenApiSchema itemsSchema)
        {
            TransformAsync(itemsSchema, context, cancellationToken);
        }

        if (schema.AllOf != null)
        {
            foreach (var allOfSchema in schema.AllOf)
            {
                if (allOfSchema is OpenApiSchema allOfSchemaTyped)
                {
                    TransformAsync(allOfSchemaTyped, context, cancellationToken);
                }
            }
        }

        if (schema.AnyOf != null)
        {
            foreach (var anyOfSchema in schema.AnyOf)
            {
                if (anyOfSchema is OpenApiSchema anyOfSchemaTyped)
                {
                    TransformAsync(anyOfSchemaTyped, context, cancellationToken);
                }
            }
        }

        if (schema.OneOf != null)
        {
            foreach (var oneOfSchema in schema.OneOf)
            {
                if (oneOfSchema is OpenApiSchema oneOfSchemaTyped)
                {
                    TransformAsync(oneOfSchemaTyped, context, cancellationToken);
                }
            }
        }

        return Task.CompletedTask;
    }
}

