using System.Net;
using System.Net.Http.Json;
using Xunit;
using Aspire.Hosting;

namespace MyApp.Tests.Integration;

public class SalesServiceTests
{
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
            Email = "admin@test.com",
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
        var response = await client.PostAsJsonAsync("/sales/api/customers", customer);
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