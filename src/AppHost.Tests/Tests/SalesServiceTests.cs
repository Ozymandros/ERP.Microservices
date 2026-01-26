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

        // Retry login mechanism for Admin user (Seeding might take time)
        for (int i = 0; i < 20; i++)
        {
            try
            {
                var response = await client.PostAsJsonAsync("/auth/api/auth/login", credentials);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadFromJsonAsync<TokenResponse>();
                    return content?.AccessToken;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Admin login failed (Attempt {i+1}): {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login attempt {i+1} threw exception: {ex.Message}");
            }
            await Task.Delay(2000); // Wait for seeder
        }

        throw new Exception("Failed to login as Admin after multiple attempts. Seeder might not have run.");
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
        var response = await client.GetAsync("/sales/api/sales/orders");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateSalesOrder_WithValidData_ReturnsCreatedStatusCode()
    {
        Console.WriteLine("Step 1: Starting AppHost...");
        // Arrange
        await using var app = await CreateAndStartAppAsync();
        var notifier = app.Services.GetRequiredService<ResourceNotificationService>();
        
        Console.WriteLine("Step 2: Waiting for services...");
        await notifier.WaitForResourceAsync("sales-service", KnownResourceStates.Running)
            .WaitAsync(TimeSpan.FromSeconds(60));
        await notifier.WaitForResourceAsync("inventory-service", KnownResourceStates.Running) // Ensure inventory is ready
            .WaitAsync(TimeSpan.FromSeconds(60));
        Console.WriteLine("Services are ready.");

        var client = app.CreateHttpClient("gateway");
        
        Console.WriteLine("Step 3: Getting Auth Token...");
        var token = await GetAuthTokenAsync(client);
        if (token != null)
        {
            Console.WriteLine("Auth Token received.");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            Console.WriteLine("Failed to get Auth Token.");
            throw new Exception("Auth verification failed");
        }

        // 1. Create a Customer
        Console.WriteLine("Step 4: Creating Customer...");
        var customerDto = new
        {
            Name = "Order Test Customer",
            Email = $"ordertest{Guid.NewGuid()}@example.com",
            Phone = "+15555555555",
            Address = new
            {
                Street = "123 Order St",
                City = "Order City",
                State = "OS",
                Country = "Order Country",
                PostalCode = "54321"
            }
        };
        var custResponse = await client.PostAsJsonAsync("/sales/api/sales/customers", customerDto);
        Console.WriteLine($"Create Customer Response: {custResponse.StatusCode}");
        if (!custResponse.IsSuccessStatusCode)
        {
             var err = await custResponse.Content.ReadAsStringAsync();
             Console.WriteLine($"Customer creation error: {err}");
        }
        Assert.Equal(HttpStatusCode.Created, custResponse.StatusCode);
        var createdCustomer = await custResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(createdCustomer);

        // 2. Create a Product (via Inventory Service)
        Console.WriteLine("Step 5: Creating Product...");
        var productDto = new
        {
            SKU = $"SKU-{Guid.NewGuid()}",
            Name = "Test Product",
            Description = "A product for testing orders",
            UnitPrice = 50.0m,
            QuantityInStock = 100,
            ReorderLevel = 10
        };
        var prodResponse = await client.PostAsJsonAsync("/inventory/api/inventory/products", productDto);
        Console.WriteLine($"Create Product Response: {prodResponse.StatusCode}");
        if (!prodResponse.IsSuccessStatusCode)
        {
             var err = await prodResponse.Content.ReadAsStringAsync();
             Console.WriteLine($"Product creation error: {err}");
        }
        Assert.Equal(HttpStatusCode.Created, prodResponse.StatusCode);
        var createdProduct = await prodResponse.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(createdProduct);

        // 3. Create Sales Order
        Console.WriteLine("Step 6: Creating Order...");
        var order = new
        {
            CustomerId = createdCustomer.Id,
            OrderDate = DateTime.UtcNow,
            Status = "Draft",
            Lines = new[]
            {
                new
                {
                    ProductId = createdProduct.Id,
                    Quantity = 2,
                    UnitPrice = 50.0m
                }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/sales/api/sales/orders", order);
        Console.WriteLine($"Create Order Response: {response.StatusCode}");
        
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdOrder = await response.Content.ReadFromJsonAsync<SalesOrderResponse>();
        Assert.NotNull(createdOrder?.Id);
        Console.WriteLine("Test Finished Successfully.");
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
        var response = await client.GetAsync("/sales/api/sales/customers");

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

    private class ProductResponse
    {
        public Guid Id { get; set; }
        public string? SKU { get; set; }
        public string? Name { get; set; }
        public decimal UnitPrice { get; set; }
    }
}