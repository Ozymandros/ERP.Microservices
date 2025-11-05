using System.Net;
using System.Net.Http.Json;
using Xunit;
using Aspire.Hosting;

namespace MyApp.Tests.Integration;

public class OrdersServiceTests
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
    public async Task GetOrders_WithValidToken_ReturnsSuccessStatusCode()
    {
        // Arrange
        await using var app = await CreateAndStartAppAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("orders-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        
        var client = app.CreateHttpClient("gateway");
        var token = await GetAuthTokenAsync(client);
        if (token != null)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        // Act
        var response = await client.GetAsync("/orders/api/orders");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WithValidData_ReturnsCreatedStatusCode()
    {
        // Arrange
        await using var app = await CreateAndStartAppAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("orders-service", KnownResourceStates.Running)
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
            OrderLines = new[]
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
        var response = await client.PostAsJsonAsync("/orders/api/orders", order);
        var createdOrder = await response.Content.ReadFromJsonAsync<OrderResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(createdOrder?.Id);
    }

    [Fact]
    public async Task UpdateOrderStatus_WithValidStatus_ReturnsSuccessStatusCode()
    {
        // Arrange
        await using var app = await CreateAndStartAppAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("orders-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        
        var client = app.CreateHttpClient("gateway");
        var token = await GetAuthTokenAsync(client);
        if (token != null)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        
        // Create an order first
        var newOrder = new
        {
            CustomerId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            Status = "Draft",
            OrderLines = new[]
            {
                new
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1,
                    UnitPrice = 99.99m
                }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/orders/api/orders", newOrder);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();

        var updateData = new
        {
            Status = "Processing"
        };

        // Act
        var response = await client.PatchAsJsonAsync($"/orders/api/orders/{createdOrder?.Id}/status", updateData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify the update
        var getResponse = await client.GetAsync($"/orders/api/orders/{createdOrder?.Id}");
        var updatedOrder = await getResponse.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal("Processing", updatedOrder?.Status);
    }

    [Fact]
    public async Task GetOrderById_WithValidId_ReturnsOrder()
    {
        // Arrange
        await using var app = await CreateAndStartAppAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("orders-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        
        var client = app.CreateHttpClient("gateway");
        var token = await GetAuthTokenAsync(client);
        if (token != null)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        
        // Create an order first
        var newOrder = new
        {
            CustomerId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            Status = "Draft",
            OrderLines = new[]
            {
                new
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1,
                    UnitPrice = 99.99m
                }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/orders/api/orders", newOrder);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();

        // Act
        var response = await client.GetAsync($"/orders/api/orders/{createdOrder?.Id}");
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(createdOrder?.Id, order?.Id);
        Assert.Equal(newOrder.CustomerId, order?.CustomerId);
    }

    private class TokenResponse
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }

    private class OrderResponse
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public string? Status { get; set; }
        public OrderLineResponse[]? OrderLines { get; set; }
    }

    private class OrderLineResponse
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}