using Aspire.Hosting;
using System.Net.Http.Json;

namespace MyApp.Tests.Integration;

public class SalesServiceTests : IAsyncLifetime
{
    private DistributedApplication? _app;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        _app = await CreateAndStartAppAsync();
        _client = _app.CreateHttpClient("sales-service");

        // Wait for all services to be healthy
        await WaitForHealthyServicesAsync();
    }

    private async Task WaitForHealthyServicesAsync()
    {
        var services = new[] { "auth-service", "sales-service" };

        foreach (var service in services)
        {
            var client = _app!.CreateHttpClient(service);
            await WaitForServiceHealthyAsync(client, service);
        }
    }

    private async Task WaitForServiceHealthyAsync(HttpClient client, string serviceName, int maxRetries = 30)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var response = await client.GetAsync("/health");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"{serviceName} is healthy");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{serviceName} not ready yet (attempt {i + 1}): {ex.Message}");
            }

            await Task.Delay(2000);
        }

        throw new TimeoutException($"Service {serviceName} did not become healthy in time");
    }

    public async Task DisposeAsync()
    {
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
    private async Task<DistributedApplication> CreateAndStartAppAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        var app = await appHost.BuildAsync();
        await app.StartAsync();
        return app;
    }

    private async Task<string?> GetAuthTokenAsync(HttpClient client)
    {
        var credentials = new
        {
            Email = "admin@myapp.local",
            Password = "Admin123!"
        };

        var response = await client.PostAsJsonAsync("/auth/api/auth/login", credentials);
        var content = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return content?.AccessToken;
    }

    [Fact]
    public async Task GetSalesOrders_WithValidToken_ReturnsSuccessStatusCode()
    {
        // Arrange
        await using var app = await CreateAndStartAppAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("sales-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));

        var client = app.CreateHttpClient("gateway");
        var token = await GetAuthTokenAsync(client);
        if (token != null)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        // Act
        var response = await client.GetAsync("/sales/api/orders");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateSalesOrder_WithValidData_ReturnsCreatedStatusCode()
    {
        // Arrange
        await using var app = await CreateAndStartAppAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("sales-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));

        var client = app.CreateHttpClient("gateway");
        var token = await GetAuthTokenAsync(client);
        if (token != null)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        var order = new
        {
            CustomerId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            Status = "Draft",
            Lines = new[]
            {
                new
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1,
                    UnitPrice = 99.99m
                }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/sales/api/orders", order);
        var createdOrder = await response.Content.ReadFromJsonAsync<SalesOrderResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(createdOrder?.Id);
    }

    [Fact]
    public async Task GetCustomers_WithValidToken_ReturnsSuccessStatusCode()
    {
        // Arrange
        await using var app = await CreateAndStartAppAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("sales-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));

        var client = app.CreateHttpClient("gateway");
        var token = await GetAuthTokenAsync(client);
        if (token != null)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        // Act
        var response = await client.GetAsync("/sales/api/customers");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateCustomer_WithValidData_ReturnsCreatedStatusCode()
    {
        // Arrange
        await using var app = await CreateAndStartAppAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("sales-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));

        var client = app.CreateHttpClient("gateway");
        var token = await GetAuthTokenAsync(client);
        if (token != null)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        var customer = new
        {
            Name = "Test Customer",
            Email = $"test{Guid.NewGuid()}@example.com",
            Phone = "+1234567890",
            Address = new
            {
                Street = "123 Test St",
                City = "Test City",
                State = "TS",
                Country = "Test Country",
                PostalCode = "12345"
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/sales/api/sales/customers", customer);
        var createdCustomer = await response.Content.ReadFromJsonAsync<CustomerResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(createdCustomer?.Id);
        Assert.Equal(customer.Email, createdCustomer?.Email);
    }

    private class TokenResponse
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }

    private class SalesOrderResponse
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public string? Status { get; set; }
        public SalesOrderLineResponse[]? Lines { get; set; }
    }

    private class SalesOrderLineResponse
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    private class CustomerResponse
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public AddressResponse? Address { get; set; }
    }

    private class AddressResponse
    {
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
    }
}