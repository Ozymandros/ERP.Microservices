using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace MyApp.Shared.Infrastructure.OpenApi;

/// <summary>
/// Transformer that fixes DateTime schema generation by ensuring proper format and avoiding default values.
/// This prevents JsonException when OpenAPI schema generation tries to serialize default(DateTime) values.
/// </summary>
public sealed class DateTimeSchemaDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        // Ensure Components exists
        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, OpenApiSchema>();

        // Fix all DateTime schemas in the document
        if (document.Components.Schemas != null)
        {
            FixDateTimeSchemas(document.Components.Schemas);
        }

        // Fix schemas in paths
        if (document.Paths != null)
        {
            foreach (var pathItem in document.Paths.Values)
            {
                if (pathItem.Operations != null)
                {
                    foreach (var operation in pathItem.Operations.Values)
                    {
                        // Fix request body schemas
                        if (operation.RequestBody?.Content != null && document.Components.Schemas != null)
                        {
                            foreach (var content in operation.RequestBody.Content.Values)
                            {
                                if (content.Schema != null && content.Schema is OpenApiSchema schema)
                                {
                                    FixSchemaRecursive(schema, document.Components.Schemas);
                                }
                            }
                        }

                        // Fix response schemas
                        if (operation.Responses != null && document.Components.Schemas != null)
                        {
                            foreach (var response in operation.Responses.Values)
                            {
                                if (response.Content != null)
                                {
                                    foreach (var content in response.Content.Values)
                                    {
                                        if (content.Schema != null && content.Schema is OpenApiSchema schema)
                                        {
                                            FixSchemaRecursive(schema, document.Components.Schemas);
                                        }
                                    }
                                }
                            }
                        }

                        // Fix parameter schemas
                        if (operation.Parameters != null && document.Components.Schemas != null)
                        {
                            foreach (var parameter in operation.Parameters)
                            {
                                if (parameter.Schema != null && parameter.Schema is OpenApiSchema schema)
                                {
                                    FixSchemaRecursive(schema, document.Components.Schemas);
                                }
                            }
                        }
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    private void FixDateTimeSchemas(IDictionary<string, OpenApiSchema> schemas)
    {
        foreach (var kvp in schemas.ToList())
        {
            if (kvp.Value is OpenApiSchema schema)
            {
                FixSchemaRecursive(schema, schemas);
            }
        }
    }

    private void FixSchemaRecursive(OpenApiSchema schema, IDictionary<string, OpenApiSchema> allSchemas)
    {
        if (schema == null) return;

        // Fix DateTime schema
        if (schema.Type == "string" && schema.Format == "date-time")
        {
            // Remove any default value that might be DateTime.MinValue
            // This prevents JsonException when OpenAPI schema generation tries to serialize default(DateTime)
            schema.Default = null;
        }

        // Recursively fix nested schemas
        if (schema.Properties != null)
        {
            foreach (var property in schema.Properties.Values)
            {
                if (property is OpenApiSchema propertySchema)
                {
                    FixSchemaRecursive(propertySchema, allSchemas);
                }
            }
        }

        if (schema.Items is OpenApiSchema itemsSchema)
        {
            FixSchemaRecursive(itemsSchema, allSchemas);
        }

        if (schema.AllOf != null)
        {
            foreach (var allOfSchema in schema.AllOf)
            {
                if (allOfSchema is OpenApiSchema allOfSchemaTyped)
                {
                    FixSchemaRecursive(allOfSchemaTyped, allSchemas);
                }
            }
        }

        if (schema.AnyOf != null)
        {
            foreach (var anyOfSchema in schema.AnyOf)
            {
                if (anyOfSchema is OpenApiSchema anyOfSchemaTyped)
                {
                    FixSchemaRecursive(anyOfSchemaTyped, allSchemas);
                }
            }
        }

        if (schema.OneOf != null)
        {
            foreach (var oneOfSchema in schema.OneOf)
            {
                if (oneOfSchema is OpenApiSchema oneOfSchemaTyped)
                {
                    FixSchemaRecursive(oneOfSchemaTyped, allSchemas);
                }
            }
        }
    }
}

