using AppHost.Tests.Base;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;

namespace AppHost.Tests.Tests;

public class PurchasingServiceTests : BaseIntegrationTest
{
    public PurchasingServiceTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task GetPurchaseOrders_WithValidToken_ReturnsSuccessStatusCode()
    {
        // Arrange
        await WaitForServiceAsync("purchasing-service");
        var client = await CreateAuthorizedClientAsync();

        // Act
        var response = await client.GetAsync("/purchasing/api/orders");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithValidData_ReturnsCreatedStatusCode()
    {
        // Arrange
        await WaitForServiceAsync("purchasing-service");
        var client = await CreateAuthorizedClientAsync();
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
        var response = await client.PostAsJsonAsync("/purchasing/api/orders", order);
        var createdOrder = await response.Content.ReadFromJsonAsync<PurchaseOrderResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(createdOrder?.Id);
    }

    [Fact]
    public async Task GetSuppliers_WithValidToken_ReturnsSuccessStatusCode()
    {
        // Arrange
        await WaitForServiceAsync("purchasing-service");
        var client = await CreateAuthorizedClientAsync();

        // Act
        var response = await client.GetAsync("/purchasing/api/suppliers");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateSupplier_WithValidData_ReturnsCreatedStatusCode()
    {
        // Arrange
        await WaitForServiceAsync("purchasing-service");
        var client = await CreateAuthorizedClientAsync();
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
        var response = await client.PostAsJsonAsync("/purchasing/api/suppliers", supplier);
        var createdSupplier = await response.Content.ReadFromJsonAsync<SupplierResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(createdSupplier?.Id);
        Assert.Equal(supplier.Email, createdSupplier.Email);
    }

    [Fact]
    public async Task UpdatePurchaseOrderStatus_WithValidStatus_ReturnsSuccessStatusCode()
    {
        // Arrange
        await WaitForServiceAsync("purchasing-service");
        var client = await CreateAuthorizedClientAsync();
        
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

        var createResponse = await client.PostAsJsonAsync("/purchasing/api/orders", newOrder);
        var createdOrder = await createResponse.Content.ReadFromJsonAsync<PurchaseOrderResponse>();

        var updateData = new
        {
            Status = "Approved"
        };

        // Act
        var response = await client.PatchAsJsonAsync($"/purchasing/api/orders/{createdOrder.Id}/status", updateData);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify the update
        var getResponse = await client.GetAsync($"/purchasing/api/orders/{createdOrder.Id}");
        var updatedOrder = await getResponse.Content.ReadFromJsonAsync<PurchaseOrderResponse>();
        Assert.Equal("Approved", updatedOrder.Status);
    }

    private class PurchaseOrderResponse
    {
        public Guid Id { get; set; }
        public Guid SupplierId { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime ExpectedDeliveryDate { get; set; }
        public string Status { get; set; }
        public PurchaseOrderLineResponse[] Lines { get; set; }
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
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public AddressResponse Address { get; set; }
        public string PaymentTerms { get; set; }
    }

    private class AddressResponse
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
    }
}