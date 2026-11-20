using Aspire.Hosting;
using System.Net.Http.Json;
using System.Text.Json;

namespace MyApp.Tests.Integration;

/// <summary>
/// Integration tests for the Projects service. The gateway has no Projects route yet, so the service is
/// called directly (projects-service) with a token obtained through the gateway; Sales is reached through
/// the gateway to create the bill-to customer.
/// </summary>
public class ProjectsServiceTests
{
    private static async Task<DistributedApplication> CreateAndStartAppAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder => clientBuilder.AddStandardResilienceHandler());

        var app = await appHost.BuildAsync();
        await app.StartAsync();
        return app;
    }

    private static async Task<string> GetAuthTokenAsync(HttpClient gateway)
    {
        var credentials = new { Email = "admin@myapp.local", Password = "Admin123!" };
        for (var i = 0; i < 20; i++)
        {
            try
            {
                var response = await gateway.PostAsJsonAsync("/auth/api/auth/login", credentials);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadFromJsonAsync<TokenResponse>();
                    if (!string.IsNullOrEmpty(content?.AccessToken))
                        return content.AccessToken;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login attempt {i + 1} failed: {ex.Message}");
            }

            await Task.Delay(2000);
        }

        throw new Exception("Failed to login as Admin.");
    }

    private static async Task<(DistributedApplication App, HttpClient Gateway, HttpClient Projects)> StartAsync()
    {
        var app = await CreateAndStartAppAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("projects-service", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(90));
        await notifier.WaitForResourceAsync("sales-service", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(90));

        var gateway = app.CreateHttpClient("gateway");
        var token = await GetAuthTokenAsync(gateway);
        var auth = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        gateway.DefaultRequestHeaders.Authorization = auth;

        var projects = app.CreateHttpClient("projects-service");
        projects.DefaultRequestHeaders.Authorization = auth;
        return (app, gateway, projects);
    }

    /// <summary>Listing projects with a valid token returns OK.</summary>
    [Fact]
    public async Task GetProjects_WithValidToken_ReturnsOk()
    {
        var (app, _, projects) = await StartAsync();
        await using var appScope = app;

        var response = await projects.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Creating a project for an unknown customer is rejected (customer validated against Sales via Dapr).</summary>
    [Fact]
    public async Task CreateProject_UnknownCustomer_ReturnsBadRequest()
    {
        var (app, _, projects) = await StartAsync();
        await using var appScope = app;

        var response = await projects.PostAsJsonAsync("/api/projects", new
        {
            Description = "Ghost customer project",
            CustomerId = Guid.NewGuid()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>Full planning flow: customer → project → task → resource planning line → statistics.</summary>
    [Fact]
    public async Task CreateProject_PlanResource_StatisticsReflectBudget()
    {
        var (app, gateway, projects) = await StartAsync();
        await using var appScope = app;

        // Customer (Sales, through the gateway)
        var customerResponse = await gateway.PostAsJsonAsync("/sales/api/sales/customers", new
        {
            Name = "Projects Test Customer",
            Email = $"projects{Guid.NewGuid():N}@example.com",
            PhoneNumber = "+34900000000",
            Address = "Carrer Major 1, Barcelona"
        });
        Assert.True(customerResponse.IsSuccessStatusCode, await customerResponse.Content.ReadAsStringAsync());
        var customerId = (await customerResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Project (local currency)
        var projectResponse = await projects.PostAsJsonAsync("/api/projects", new
        {
            Description = "ERP rollout",
            CustomerId = customerId
        });
        Assert.Equal(HttpStatusCode.Created, projectResponse.StatusCode);
        var projectId = (await projectResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Task
        var taskResponse = await projects.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new
        {
            TaskNo = "1000",
            Description = "Analysis"
        });
        Assert.Equal(HttpStatusCode.Created, taskResponse.StatusCode);
        var taskId = (await taskResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Seeded resource R0001
        var resources = await projects.GetFromJsonAsync<JsonElement>("/api/projects/resources");
        var resourceId = resources.EnumerateArray().First(r => r.GetProperty("resourceNo").GetString() == "R0001")
            .GetProperty("id").GetGuid();

        // Planning line: 10 hours, BudgetAndBillable (enum value 2), Resource (enum value 0)
        var lineResponse = await projects.PostAsJsonAsync($"/api/projects/{projectId}/planning-lines", new
        {
            ProjectTaskId = taskId,
            LineType = 2,
            Kind = 0,
            ResourceId = resourceId,
            Quantity = 10m
        });
        Assert.Equal(HttpStatusCode.Created, lineResponse.StatusCode);

        // Statistics
        var stats = await projects.GetFromJsonAsync<JsonElement>($"/api/projects/{projectId}/statistics");
        var totals = stats.GetProperty("totals");
        Assert.True(totals.GetProperty("budgetCostLcy").GetDecimal() > 0);
        Assert.Equal(totals.GetProperty("budgetPrice").GetDecimal(), totals.GetProperty("billablePrice").GetDecimal());
    }

    private sealed class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
    }
}
