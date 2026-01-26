using System.Net;
using System.Net.Http.Json;
using Xunit;
using Aspire.Hosting;

namespace MyApp.Tests.Integration;

public class PurchasingServiceTests
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
    public async Task GetPurchaseOrders_WithValidToken_ReturnsSuccessStatusCode()
    {
        // Arrange
        await using var app = await CreateAndStartAppAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("purchasing-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        
        var client = app.CreateHttpClient("gateway");
        var token = await GetAuthTokenAsync(client);
        if (token != null)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        // Act
        var response = await client.GetAsync("/purchasing/api/purchasing/orders");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithValidData_ReturnsCreatedStatusCode()
    {
        // Arrange
        await using var app = await CreateAndStartAppAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("purchasing-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        
        var client = app.CreateHttpClient("gateway");
        var token = await GetAuthTokenAsync(client);
        if (token != null)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        
        var order = new
        {
            SupplierId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Status = "Draft",
            Lines = new[]
            {
                new
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 10,
                    UnitPrice = 49.99m
                }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/purchasing/api/purchasing/orders", order);
        var createdOrder = await response.Content.ReadFromJsonAsync<PurchaseOrderResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(createdOrder?.Id);
    }

    [Fact]
    public async Task GetSuppliers_WithValidToken_ReturnsSuccessStatusCode()
    {
        // Arrange
        await using var app = await CreateAndStartAppAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("purchasing-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        
        var client = app.CreateHttpClient("gateway");
        var token = await GetAuthTokenAsync(client);
        if (token != null)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        // Act
        var response = await client.GetAsync("/purchasing/api/purchasing/suppliers");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateSupplier_WithValidData_ReturnsCreatedStatusCode()
    {
        // Arrange
        await using var app = await CreateAndStartAppAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("purchasing-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        
        var client = app.CreateHttpClient("gateway");
        var token = await GetAuthTokenAsync(client);
        if (token != null)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        
        var supplier = new
        {
            Name = "Test Supplier",
            Email = $"supplier{Guid.NewGuid()}@test.com",
            Phone = "+1234567890",
            Address = new
            {
                Street = "456 Supplier St",
                City = "Supplier City",
                State = "SP",
                Country = "Supplier Country",
                PostalCode = "54321"
            },
            PaymentTerms = "Net 30"
        };

        // Act
        var response = await client.PostAsJsonAsync("/purchasing/api/purchasing/suppliers", supplier);
        var createdSupplier = await response.Content.ReadFromJsonAsync<SupplierResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(createdSupplier?.Id);
        Assert.Equal(supplier.Email, createdSupplier?.Email);
    }

    [Fact]
    public async Task UpdatePurchaseOrderStatus_WithValidStatus_ReturnsSuccessStatusCode()
    {
        // Arrange
        await using var app = await CreateAndStartAppAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("purchasing-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        
        var client = app.CreateHttpClient("gateway");
        var token = await GetAuthTokenAsync(client);
        if (token != null)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        
        // Create a purchase order first
        var newOrder = new
        {
            SupplierId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(7),
            Status = "Draft",
            Lines = new[]
            {
                new
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 10,
                    UnitPrice = 49.99m
                }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/purchasing/api/purchasing/orders", newOrder);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<PurchaseOrderResponse>();

        var updateData = new
        {
            Status = "Approved"
        };

        // Act
        var response = await client.PatchAsJsonAsync($"/purchasing/api/purchasing/orders/{createdOrder?.Id}/status", updateData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify the update
        var getResponse = await client.GetAsync($"/purchasing/api/purchasing/orders/{createdOrder?.Id}");
        var updatedOrder = await getResponse.Content.ReadFromJsonAsync<PurchaseOrderResponse>();
        Assert.Equal("Approved", updatedOrder?.Status);
    }

    private class TokenResponse
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }

    private class PurchaseOrderResponse
    {
        public Guid Id { get; set; }
        public Guid SupplierId { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime ExpectedDeliveryDate { get; set; }
        public string? Status { get; set; }
        public PurchaseOrderLineResponse[]? Lines { get; set; }
    }

    private class PurchaseOrderLineResponse
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    private class SupplierResponse
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public AddressResponse? Address { get; set; }
        public string? PaymentTerms { get; set; }
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