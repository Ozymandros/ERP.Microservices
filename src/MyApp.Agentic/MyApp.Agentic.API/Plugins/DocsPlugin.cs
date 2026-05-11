using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace MyApp.Agentic.API.Plugins;

public class DocsPlugin
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DocsPlugin> _logger;
    private readonly string _docsBaseUrl;

    public DocsPlugin(IHttpClientFactory httpClientFactory, ILogger<DocsPlugin> logger, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient("DocsPlugin");
        _logger = logger;
        _docsBaseUrl = configuration["Docs:BaseUrl"] 
            ?? "https://ozymandros.github.io/ERP.Microservices/";
    }

    [Description("Search documentation by keyword")]
    public async Task<string> SearchAsync(string query)
    {
        try
        {
            _logger.LogInformation("Searching docs for: {Query}", query);

            var searchResults = new List<DocSearchResult>();

            var indexUrl = $"{_docsBaseUrl}/api/index.json";
            var indexResponse = await _httpClient.GetAsync(indexUrl);

            if (indexResponse.IsSuccessStatusCode)
            {
                var apiIndex = await indexResponse.Content.ReadFromJsonAsync<DocApiIndex>();
                if (apiIndex?.Documents != null)
                {
                    var results = apiIndex.Documents
                        .Where(d => 
                            d.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            (d.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (d.SearchKeywords?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
                        .Take(10)
                        .Select(d => new DocSearchResult 
                        { 
                            Name = d.Name ?? string.Empty, 
                            Title = d.Title ?? string.Empty, 
                            Href = d.Href ?? string.Empty, 
                            Summary = d.Summary ?? string.Empty 
                        })
                        .ToList();

                    searchResults.AddRange(results);
                }
            }

            if (searchResults.Count == 0)
            {
                return JsonSerializer.Serialize(new { 
                    message = "No results found", 
                    query = query,
                    hint = "Try searching for: API, Agent, CRM, Billing, Configuration"
                });
            }

            _logger.LogInformation("Found {Count} results for: {Query}", searchResults.Count, query);
            return JsonSerializer.Serialize(searchResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching docs for: {Query}", query);
            return JsonSerializer.Serialize(new { 
                error = "Search failed", 
                message = "Unable to search documentation. Please try again." 
            });
        }
    }

    [Description("Get documentation for a specific topic")]
    public async Task<string> GetTopicAsync(string topic)
    {
        try
        {
            _logger.LogInformation("Getting docs topic: {Topic}", topic);

            var topicKey = topic.ToLowerInvariant().Replace(" ", "-");
            var tocUrl = $"{_docsBaseUrl}/api/toc/{topicKey}.json";
            
            var pageUrl = topicKey switch
            {
                "agentic" or "agent" => $"{_docsBaseUrl}/agentic-api/index.html",
                "api" => $"{_docsBaseUrl}/api/index.html",
                "architecture" => $"{_docsBaseUrl}/architecture/index.html",
                "deployment" => $"{_docsBaseUrl}/deployment/index.html",
                "development" => $"{_docsBaseUrl}/development/index.html",
                "security" => $"{_docsBaseUrl}/security/index.html",
                _ => $"{_docsBaseUrl}/api/{topicKey}.html"
            };

            var response = await _httpClient.GetAsync(pageUrl);
            
            if (!response.IsSuccessStatusCode)
            {
                return JsonSerializer.Serialize(new { 
                    error = "Topic not found",
                    topic = topic,
                    availableTopics = new[] { "Agentic", "API", "Architecture", "Deployment", "Development", "Security" }
                });
            }

            var content = await response.Content.ReadAsStringAsync();
            
            var titleMatch = System.Text.RegularExpressions.Regex.Match(
                content, 
                @"<title>([^<]+)</title>");
            
            var extractedTitle = titleMatch.Success ? titleMatch.Groups[1].Value : topic;

            return JsonSerializer.Serialize(new 
            { 
                topic = topic,
                url = pageUrl,
                title = extractedTitle,
                summary = $"Documentation for {topic}. Visit {pageUrl} for full details."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting docs topic: {Topic}", topic);
            return JsonSerializer.Serialize(new { 
                error = "Failed to retrieve topic",
                message = "Unable to fetch documentation. Please try again."
            });
        }
    }

    [Description("Get API reference documentation")]
    public async Task<string> GetApiReferenceAsync(string? namespaceOrClass = null)
    {
        try
        {
            _logger.LogInformation("Getting API reference: {Namespace}", namespaceOrClass ?? "all");

            var apiIndexUrl = $"{_docsBaseUrl}/api/index.json";
            var response = await _httpClient.GetAsync(apiIndexUrl);

            if (!response.IsSuccessStatusCode)
            {
                return JsonSerializer.Serialize(new { 
                    error = "API reference unavailable",
                    message = "The API documentation is not currently available."
                });
            }

            var apiIndex = await response.Content.ReadFromJsonAsync<DocApiIndex>();
            
            if (string.IsNullOrEmpty(namespaceOrClass))
            {
                var namespaces = apiIndex?.Documents
                    ?.Select(d => d.Namespace)
                    .Distinct()
                    .OrderBy(n => n)
                    .Take(20)
                    .ToList();

                return JsonSerializer.Serialize(new 
                { 
                    type = "namespace-list",
                    namespaces = namespaces,
                    total = namespaces?.Count ?? 0
                });
            }
            else
            {
                var classes = apiIndex?.Documents
                    ?.Where(d => d.Namespace?.Contains(namespaceOrClass, StringComparison.OrdinalIgnoreCase) ?? false)
                    .Take(20)
                    .Select(d => new { name = d.Name, href = d.Href, summary = d.Summary })
                    .ToList();

                return JsonSerializer.Serialize(new 
                { 
                    filter = namespaceOrClass,
                    classes = classes,
                    count = classes?.Count ?? 0
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API reference");
            return JsonSerializer.Serialize(new { 
                error = "Failed to retrieve API reference",
                message = "Please try again later."
            });
        }
    }

    [Description("Get list of all documentation sections")]
    public async Task<string> GetSectionsAsync()
    {
        try
        {
            var sections = new[]
            {
                new { id = "guid", name = "Guides", description = "Quick starts and how-tos", url = $"{_docsBaseUrl}/guides/index.html" },
                new { id = "architecture", name = "Architecture", description = "System design and patterns", url = $"{_docsBaseUrl}/architecture/index.html" },
                new { id = "deployment", name = "Deployment", description = "Deployment guides and CI/CD", url = $"{_docsBaseUrl}/deployment/index.html" },
                new { id = "development", name = "Development", description = "Developer guides", url = $"{_docsBaseUrl}/development/index.html" },
                new { id = "security", name = "Security", description = "Security best practices", url = $"{_docsBaseUrl}/security/index.html" },
                new { id = "agentic", name = "Agentic Module", description = "AI agents and plugins", url = $"{_docsBaseUrl}/agentic-api/index.html" },
                new { id = "api", name = "API Reference", description = "Full API documentation", url = $"{_docsBaseUrl}/api/index.html" }
            };

            return JsonSerializer.Serialize(sections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sections");
            return JsonSerializer.Serialize(new { error = "Failed to retrieve sections" });
        }
    }
}

public class DocSearchResult
{
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Href { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}

public class DocApiIndex
{
    public List<DocApiDocument>? Documents { get; set; }
}

public class DocApiDocument
{
    public string Name { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Href { get; set; }
    public string? Summary { get; set; }
    public string? Namespace { get; set; }
    public string? SearchKeywords { get; set; }
}
