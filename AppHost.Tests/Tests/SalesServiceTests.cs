using AppHost.Tests.Base;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;

namespace AppHost.Tests.Tests;

public class SalesServiceTests : BaseIntegrationTest
{
    public SalesServiceTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task GetSalesOrders_WithValidToken_ReturnsSuccessStatusCode()
    {
        // Arrange
        await WaitForServiceAsync("sales-service");
        var client = await CreateAuthorizedClientAsync();

        // Act
        var response = await client.GetAsync("/sales/api/orders");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateSalesOrder_WithValidData_ReturnsCreatedStatusCode()
    {
        // Arrange
        await WaitForServiceAsync("sales-service");
        var client = await CreateAuthorizedClientAsync();
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
        await WaitForServiceAsync("sales-service");
        var client = await CreateAuthorizedClientAsync();

        // Act
        var response = await client.GetAsync("/sales/api/customers");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateCustomer_WithValidData_ReturnsCreatedStatusCode()
    {
        // Arrange
        await WaitForServiceAsync("sales-service");
        var client = await CreateAuthorizedClientAsync();
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
        Assert.Equal(customer.Email, createdCustomer.Email);
    }

    private class SalesOrderResponse
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public SalesOrderLineResponse[] Lines { get; set; }
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
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public AddressResponse Address { get; set; }
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