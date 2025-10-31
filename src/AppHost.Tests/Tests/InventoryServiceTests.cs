using System.Net;
using System.Net.Http.Json;
using Xunit;
using Aspire.Hosting;

namespace MyApp.Tests.Integration;

public class InventoryServiceTests
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

    private class TokenResponse
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }

    [Fact]
    public async Task GetProducts_WithValidToken_ReturnsSuccessStatusCode()
    {
        // Arrange
        await using var app = await CreateAndStartAppAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("inventory-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        
        var client = app.CreateHttpClient("gateway");
        var token = await GetAuthTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/inventory/api/products");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WithValidData_ReturnsCreatedStatusCode()
    {
        // Arrange
        await using var app = await CreateAndStartAppAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("inventory-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        
        var client = app.CreateHttpClient("gateway");
        var token = await GetAuthTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        var product = new
        {
            SKU = $"TEST-{Guid.NewGuid()}",
            Name = "Test Product",
            Description = "Test Description",
            UnitPrice = 10.99m,
            QuantityInStock = 100,
            ReorderLevel = 10
        };

        // Act
        var response = await client.PostAsJsonAsync("/inventory/api/products", product);
        var createdProduct = await response.Content.ReadFromJsonAsync<ProductResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(createdProduct?.Id);
        Assert.Equal(product.SKU, createdProduct.SKU);
    }

    [Fact]
    public async Task GetProductById_WithValidId_ReturnsProduct()
    {
        // Arrange
        await using var app = await CreateAndStartAppAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("inventory-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        
        var client = app.CreateHttpClient("gateway");
        var token = await GetAuthTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        // Create a product first
        var newProduct = new
        {
            SKU = $"TEST-{Guid.NewGuid()}",
            Name = "Test Product",
            Description = "Test Description",
            UnitPrice = 10.99m,
            QuantityInStock = 100,
            ReorderLevel = 10
        };

        var createResponse = await client.PostAsJsonAsync("/inventory/api/products", newProduct);
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        // Act
        var response = await client.GetAsync($"/inventory/api/products/{createdProduct?.Id}");
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(createdProduct?.Id, product?.Id);
        Assert.Equal(newProduct.SKU, product?.SKU);
    }

    [Fact]
    public async Task UpdateProduct_WithValidData_ReturnsSuccessStatusCode()
    {
        // Arrange
        await using var app = await CreateAndStartAppAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("inventory-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        
        var client = app.CreateHttpClient("gateway");
        var token = await GetAuthTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        // Create a product first
        var newProduct = new
        {
            SKU = $"TEST-{Guid.NewGuid()}",
            Name = "Test Product",
            Description = "Test Description",
            UnitPrice = 10.99m,
            QuantityInStock = 100,
            ReorderLevel = 10
        };

        var createResponse = await client.PostAsJsonAsync("/inventory/api/products", newProduct);
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        var updateData = new
        {
            Id = createdProduct?.Id,
            SKU = createdProduct?.SKU,
            Name = "Updated Product Name",
            Description = "Updated Description",
            UnitPrice = 15.99m,
            QuantityInStock = 150,
            ReorderLevel = 15
        };

        // Act
        var response = await client.PutAsJsonAsync($"/inventory/api/products/{createdProduct?.Id}", updateData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify the update
        var getResponse = await client.GetAsync($"/inventory/api/products/{createdProduct?.Id}");
        var updatedProduct = await getResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.Equal(updateData.Name, updatedProduct?.Name);
        Assert.Equal(updateData.UnitPrice, updatedProduct?.UnitPrice);
    }

    [Fact]
    public async Task DeleteProduct_WithValidId_ReturnsNoContentStatusCode()
    {
        // Arrange
        await using var app = await CreateAndStartAppAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifier.WaitForResourceAsync("inventory-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(30));
        
        var client = app.CreateHttpClient("gateway");
        var token = await GetAuthTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        // Create a product first
        var newProduct = new
        {
            SKU = $"TEST-{Guid.NewGuid()}",
            Name = "Test Product",
            Description = "Test Description",
            UnitPrice = 10.99m,
            QuantityInStock = 100,
            ReorderLevel = 10
        };

        var createResponse = await client.PostAsJsonAsync("/inventory/api/products", newProduct);
        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        // Act
        var response = await client.DeleteAsync($"/inventory/api/products/{createdProduct?.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the delete
        var getResponse = await client.GetAsync($"/inventory/api/products/{createdProduct?.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private class ProductResponse
    {
        public Guid Id { get; set; }
        public string? SKU { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal UnitPrice { get; set; }
        public int QuantityInStock { get; set; }
        public int ReorderLevel { get; set; }
    }
}