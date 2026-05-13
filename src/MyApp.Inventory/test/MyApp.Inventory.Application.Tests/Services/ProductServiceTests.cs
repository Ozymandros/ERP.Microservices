using AutoMapper;
using FluentAssertions;
using Moq;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Services;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;
using MyApp.Inventory.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;
using Xunit;

namespace MyApp.Inventory.Application.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        _mockProductRepository = new Mock<IProductRepository>();
        _mockMapper = new Mock<IMapper>();

        _productService = new ProductService(
            _mockProductRepository.Object,
            _mockMapper.Object);
    }

    #region GetProductByIdAsync Tests

    [Fact]
    public async Task GetProductByIdAsync_WithExistingId_ReturnsProductDto()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product(Guid.NewGuid()) { SKU = "PRD-001", Name = "Test Product" };
        var expectedDto = new ProductDto(Guid.NewGuid())
        {
            SKU = "PRD-001",
            Name = "Test Product"
        };

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockMapper.Setup(m => m.Map<ProductDto>(product)).Returns(expectedDto);

        // Act
        var result = await _productService.GetProductByIdAsync(productId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PRD-001", result.SKU);
        _mockProductRepository.Verify(r => r.GetByIdAsync(productId), Times.Once);
    }

    [Fact]
    public async Task GetProductByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync((Product?)null);

        // Act
        var result = await _productService.GetProductByIdAsync(productId);

        // Assert
        Assert.Null(result);
        _mockProductRepository.Verify(r => r.GetByIdAsync(productId), Times.Once);
    }

    #endregion

    #region GetProductBySkuAsync Tests

    [Fact]
    public async Task GetProductBySkuAsync_WithExistingSku_ReturnsProductDto()
    {
        // Arrange
        var sku = "PRD-001";
        var product = new Product(Guid.NewGuid()) { SKU = sku, Name = "Test Product" };
        var expectedDto = new ProductDto(Guid.NewGuid())
        {
            SKU = sku,
            Name = "Test Product"
        };

        _mockProductRepository.Setup(r => r.GetBySkuAsync(sku)).ReturnsAsync(product);
        _mockMapper.Setup(m => m.Map<ProductDto>(product)).Returns(expectedDto);

        // Act
        var result = await _productService.GetProductBySkuAsync(sku);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(sku, result.SKU);
        _mockProductRepository.Verify(r => r.GetBySkuAsync(sku), Times.Once);
    }

    [Fact]
    public async Task GetProductBySkuAsync_WithNonExistentSku_ReturnsNull()
    {
        // Arrange
        var sku = "NONEXISTENT";
        _mockProductRepository.Setup(r => r.GetBySkuAsync(sku)).ReturnsAsync((Product?)null);

        // Act
        var result = await _productService.GetProductBySkuAsync(sku);

        // Assert
        Assert.Null(result);
        _mockProductRepository.Verify(r => r.GetBySkuAsync(sku), Times.Once);
    }

    #endregion

    #region GetProductByNameAsync Tests

    [Fact]
    public async Task GetProductByNameAsync_WithExistingName_ReturnsProductDto()
    {
        // Arrange
        var name = "Test Product";
        var product = new Product(Guid.NewGuid()) { SKU = "PRD-001", Name = name };
        var expectedDto = new ProductDto(Guid.NewGuid())
        {
            SKU = "PRD-001",
            Name = name
        };

        _mockProductRepository.Setup(r => r.GetByNameAsync(name)).ReturnsAsync(product);
        _mockMapper.Setup(m => m.Map<ProductDto>(product)).Returns(expectedDto);

        // Act
        var result = await _productService.GetProductByNameAsync(name);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(name, result.Name);
        _mockProductRepository.Verify(r => r.GetByNameAsync(name), Times.Once);
    }

    [Fact]
    public async Task GetProductByNameAsync_WithNonExistentName_ReturnsNull()
    {
        // Arrange
        var name = "Non-Existent Product";
        _mockProductRepository.Setup(r => r.GetByNameAsync(name)).ReturnsAsync((Product?)null);

        // Act
        var result = await _productService.GetProductByNameAsync(name);

        // Assert
        Assert.Null(result);
        _mockProductRepository.Verify(r => r.GetByNameAsync(name), Times.Once);
    }

    #endregion

    #region GetAllProductsAsync Tests

    [Fact]
    public async Task GetAllProductsAsync_ReturnsAllProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product(Guid.NewGuid()) { SKU = "PRD-001", Name = "Product 1" },
            new Product(Guid.NewGuid()) { SKU = "PRD-002", Name = "Product 2" }
        };

        var productDtos = new List<ProductDto>
        {
            new ProductDto(Guid.NewGuid())
            {
                SKU = "PRD-001",
                Name = "Product 1"
            },
            new ProductDto(Guid.NewGuid())
            {
                SKU = "PRD-002",
                Name = "Product 2"
            }
        };

        _mockProductRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(products);
        _mockMapper.Setup(m => m.Map<IEnumerable<ProductDto>>(products)).Returns(productDtos);

        // Act
        var result = await _productService.GetAllProductsAsync();

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.Contains(resultList, p => p.SKU == "PRD-001");
        _mockProductRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    #endregion

    #region GetLowStockProductsAsync Tests

    [Fact]
    public async Task GetLowStockProductsAsync_ReturnsLowStockProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product(Guid.NewGuid()) { SKU = "PRD-LOW", Name = "Low Stock Product" }
        };

        var productDtos = new List<ProductDto>
        {
            new ProductDto(Guid.NewGuid())
            {
                SKU = "PRD-LOW",
                Name = "Low Stock Product"
            }
        };

        _mockProductRepository.Setup(r => r.GetLowStockProductsAsync()).ReturnsAsync(products);
        _mockMapper.Setup(m => m.Map<IEnumerable<ProductDto>>(products)).Returns(productDtos);

        // Act
        var result = await _productService.GetLowStockProductsAsync();

        // Assert
        Assert.Single(result);
        _mockProductRepository.Verify(r => r.GetLowStockProductsAsync(), Times.Once);
    }

    #endregion

    #region CreateProductAsync Tests

    [Fact]
    public async Task CreateProductAsync_WithUniqueSku_CreatesProduct()
    {
        // Arrange
        var dto = new CreateUpdateProductDto("PRD-NEW", "New Product");
        var product = new Product(Guid.NewGuid()) { SKU = "PRD-NEW", Name = "New Product" };
        var createdProduct = new Product(Guid.NewGuid()) { SKU = "PRD-NEW", Name = "New Product" };
        var expectedDto = new ProductDto(Guid.NewGuid())
        {
            SKU = "PRD-NEW",
            Name = "New Product"
        };

        _mockProductRepository.Setup(r => r.GetBySkuAsync(dto.SKU)).ReturnsAsync((Product?)null);
        _mockMapper.Setup(m => m.Map<Product>(dto)).Returns(product);
        _mockProductRepository.Setup(r => r.AddAsync(product)).ReturnsAsync(createdProduct);
        _mockMapper.Setup(m => m.Map<ProductDto>(createdProduct)).Returns(expectedDto);

        // Act
        var result = await _productService.CreateProductAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("PRD-NEW", result.SKU);
        _mockProductRepository.Verify(r => r.GetBySkuAsync(dto.SKU), Times.Once);
        _mockProductRepository.Verify(r => r.AddAsync(product), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_WithDuplicateSku_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new CreateUpdateProductDto("PRD-DUPLICATE", "Product");
        var existingProduct = new Product(Guid.NewGuid()) { SKU = "PRD-DUPLICATE" };

        _mockProductRepository.Setup(r => r.GetBySkuAsync(dto.SKU)).ReturnsAsync(existingProduct);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _productService.CreateProductAsync(dto));

        Assert.Contains("already exists", exception.Message);
        _mockProductRepository.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Never);
    }

    #endregion

    #region UpdateProductAsync Tests

    [Fact]
    public async Task UpdateProductAsync_WithExistingProduct_UpdatesSuccessfully()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = new Product(productId) { SKU = "PRD-OLD", Name = "Old Name" };
        var updateDto = new CreateUpdateProductDto("PRD-OLD", "New Name");
        var updatedProduct = new Product(productId) { SKU = "PRD-OLD", Name = "New Name" };
        var expectedDto = new ProductDto(Guid.NewGuid())
        {
            SKU = "PRD-OLD",
            Name = "New Name"
        };

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(existingProduct);
        _mockMapper.Setup(m => m.Map(updateDto, existingProduct));
        _mockProductRepository.Setup(r => r.UpdateAsync(existingProduct)).ReturnsAsync(updatedProduct);
        _mockMapper.Setup(m => m.Map<ProductDto>(updatedProduct)).Returns(expectedDto);

        // Act
        var result = await _productService.UpdateProductAsync(productId, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        _mockProductRepository.Verify(r => r.UpdateAsync(existingProduct), Times.Once);
    }

    [Fact]
    public async Task UpdateProductAsync_WithNonExistentProduct_ThrowsKeyNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var updateDto = new CreateUpdateProductDto("PRD-001", "Product");


        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync((Product?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _productService.UpdateProductAsync(productId, updateDto));

        Assert.Contains("not found", exception.Message);
        _mockProductRepository.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductAsync_WithDuplicateSku_ThrowsInvalidOperationException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = new Product(Guid.NewGuid()) { SKU = "PRD-OLD" };
        var updateDto = new CreateUpdateProductDto("PRD-NEW", "Product");
        var conflictingProduct = new Product(Guid.NewGuid()) { SKU = "PRD-NEW" };

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(existingProduct);
        _mockProductRepository.Setup(r => r.GetBySkuAsync(updateDto.SKU)).ReturnsAsync(conflictingProduct);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _productService.UpdateProductAsync(productId, updateDto));

        Assert.Contains("already exists", exception.Message);
        _mockProductRepository.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
    }

    #endregion

    #region DeleteProductAsync Tests

    [Fact]
    public async Task DeleteProductAsync_WithExistingProduct_DeletesProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product(Guid.NewGuid()) { SKU = "PRD-001" };

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);

        // Act
        await _productService.DeleteProductAsync(productId);

        // Assert
        _mockProductRepository.Verify(r => r.GetByIdAsync(productId), Times.Once);
        _mockProductRepository.Verify(r => r.DeleteAsync(product), Times.Once);
    }

    [Fact]
    public async Task DeleteProductAsync_WithNonExistentProduct_ThrowsKeyNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync((Product?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _productService.DeleteProductAsync(productId));

        Assert.Contains("not found", exception.Message);
        _mockProductRepository.Verify(r => r.DeleteAsync(It.IsAny<Product>()), Times.Never);
    }

    #endregion

    #region GetAllProductsPaginatedAsync Tests

    [Fact]
    public async Task GetAllProductsPaginatedAsync_WithValidPagination_ReturnsPaginatedResult()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product(Guid.NewGuid()) { SKU = "PRD-001", Name = "Product 1" },
            new Product(Guid.NewGuid()) { SKU = "PRD-002", Name = "Product 2" },
            new Product(Guid.NewGuid()) { SKU = "PRD-003", Name = "Product 3" }
        };

        var paginatedProducts = new PaginatedResult<Product>(
            products.Take(2),
            1,
            2,
            3
        );

        var productDtos = new List<ProductDto>
        {
            new ProductDto(Guid.NewGuid()) { SKU = "PRD-001", Name = "Product 1" },
            new ProductDto(Guid.NewGuid()) { SKU = "PRD-002", Name = "Product 2" }
        };

        _mockProductRepository.Setup(r => r.GetAllPaginatedAsync(1, 2)).ReturnsAsync(paginatedProducts);
        _mockMapper.Setup(m => m.Map<IEnumerable<ProductDto>>(It.IsAny<IEnumerable<Product>>())).Returns(productDtos);

        // Act
        var result = await _productService.GetAllProductsPaginatedAsync(1, 2);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalCount.Should().Be(3);
        _mockProductRepository.Verify(r => r.GetAllPaginatedAsync(1, 2), Times.Once);
    }

    [Fact]
    public async Task GetAllProductsPaginatedAsync_WithSecondPage_ReturnsCorrectPage()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product(Guid.NewGuid()) { SKU = "PRD-003", Name = "Product 3" }
        };

        var paginatedProducts = new PaginatedResult<Product>(
            products,
            2,
            2,
            3
        );

        var productDtos = new List<ProductDto>
        {
            new ProductDto(Guid.NewGuid()) { SKU = "PRD-003", Name = "Product 3" }
        };

        _mockProductRepository.Setup(r => r.GetAllPaginatedAsync(2, 2)).ReturnsAsync(paginatedProducts);
        _mockMapper.Setup(m => m.Map<IEnumerable<ProductDto>>(It.IsAny<IEnumerable<Product>>())).Returns(productDtos);

        // Act
        var result = await _productService.GetAllProductsPaginatedAsync(2, 2);

        // Assert
        result.Should().NotBeNull();
        result.PageNumber.Should().Be(2);
        result.Items.Should().HaveCount(1);
        _mockProductRepository.Verify(r => r.GetAllPaginatedAsync(2, 2), Times.Once);
    }

    [Fact]
    public async Task GetAllProductsPaginatedAsync_WithEmptyResult_ReturnsEmptyPaginatedResult()
    {
        // Arrange
        var paginatedProducts = new PaginatedResult<Product>(
            Enumerable.Empty<Product>(),
            1,
            20,
            0
        );

        _mockProductRepository.Setup(r => r.GetAllPaginatedAsync(1, 20)).ReturnsAsync(paginatedProducts);
        _mockMapper.Setup(m => m.Map<IEnumerable<ProductDto>>(It.IsAny<IEnumerable<Product>>())).Returns(Enumerable.Empty<ProductDto>());

        // Act
        var result = await _productService.GetAllProductsPaginatedAsync(1, 20);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    #region QueryProductsAsync Tests

    [Fact]
    public async Task QueryProductsAsync_WithSearchTerm_ReturnsFilteredResults()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product(Guid.NewGuid()) { SKU = "PRD-SEARCH-001", Name = "Search Product" }
        };

        var querySpec = new QuerySpec { SearchTerm = "SEARCH" };
        var spec = new ProductQuerySpec(querySpec);
        var paginatedResult = new PaginatedResult<Product>(products, 1, 20, 1);

        var productDtos = new List<ProductDto>
        {
            new ProductDto(Guid.NewGuid()) { SKU = "PRD-SEARCH-001", Name = "Search Product" }
        };

        _mockProductRepository.Setup(r => r.QueryAsync(spec)).ReturnsAsync(paginatedResult);
        _mockMapper.Setup(m => m.Map<ProductDto>(It.IsAny<Product>())).Returns((Product p) =>
            new ProductDto(p.Id) { SKU = p.SKU, Name = p.Name });

        // Act
        var result = await _productService.QueryProductsAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        _mockProductRepository.Verify(r => r.QueryAsync(spec), Times.Once);
    }

    [Fact]
    public async Task QueryProductsAsync_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product(Guid.NewGuid()) { SKU = "PRD-001", Name = "Product", UnitPrice = 10.00m }
        };

        var querySpec = new QuerySpec();
        querySpec.Filters = new Dictionary<string, string> { { "UnitPriceMin", "5" }, { "UnitPriceMax", "15" } };
        var spec = new ProductQuerySpec(querySpec);
        var paginatedResult = new PaginatedResult<Product>(products, 1, 20, 1);

        var productDtos = new List<ProductDto>
        {
            new ProductDto(Guid.NewGuid()) { SKU = "PRD-001", Name = "Product", UnitPrice = 10.00m }
        };

        _mockProductRepository.Setup(r => r.QueryAsync(spec)).ReturnsAsync(paginatedResult);
        _mockMapper.Setup(m => m.Map<ProductDto>(It.IsAny<Product>())).Returns((Product p) =>
            new ProductDto(p.Id) { SKU = p.SKU, Name = p.Name, UnitPrice = p.UnitPrice });

        // Act
        var result = await _productService.QueryProductsAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        _mockProductRepository.Verify(r => r.QueryAsync(spec), Times.Once);
    }

    #endregion

    #region Edge Cases and Error Scenarios

    [Fact]
    public async Task CreateProductAsync_WithEmptySku_CreatesProduct()
    {
        // Arrange
        var dto = new CreateUpdateProductDto("", "Product Name");
        var product = new Product(Guid.NewGuid()) { SKU = "", Name = "Product Name" };
        var expectedDto = new ProductDto(Guid.NewGuid()) { SKU = "", Name = "Product Name" };

        _mockProductRepository.Setup(r => r.GetBySkuAsync("")).ReturnsAsync((Product?)null);
        _mockMapper.Setup(m => m.Map<Product>(dto)).Returns(product);
        _mockProductRepository.Setup(r => r.AddAsync(product)).ReturnsAsync(product);
        _mockMapper.Setup(m => m.Map<ProductDto>(product)).Returns(expectedDto);

        // Act
        var result = await _productService.CreateProductAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.SKU.Should().Be("");
    }

    [Fact]
    public async Task CreateProductAsync_WithWhitespaceSku_CreatesProduct()
    {
        // Arrange
        var dto = new CreateUpdateProductDto("   ", "Product Name");
        var product = new Product(Guid.NewGuid()) { SKU = "   ", Name = "Product Name" };
        var expectedDto = new ProductDto(Guid.NewGuid()) { SKU = "   ", Name = "Product Name" };

        _mockProductRepository.Setup(r => r.GetBySkuAsync("   ")).ReturnsAsync((Product?)null);
        _mockMapper.Setup(m => m.Map<Product>(dto)).Returns(product);
        _mockProductRepository.Setup(r => r.AddAsync(product)).ReturnsAsync(product);
        _mockMapper.Setup(m => m.Map<ProductDto>(product)).Returns(expectedDto);

        // Act
        var result = await _productService.CreateProductAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.SKU.Should().Be("   ");
    }

    [Fact]
    public async Task CreateProductAsync_WithSpecialCharactersInSku_CreatesProduct()
    {
        // Arrange
        var sku = "PRD-001-SPECIAL!@#";
        var dto = new CreateUpdateProductDto(sku, "Product");
        var product = new Product(Guid.NewGuid()) { SKU = sku, Name = "Product" };
        var expectedDto = new ProductDto(Guid.NewGuid()) { SKU = sku, Name = "Product" };

        _mockProductRepository.Setup(r => r.GetBySkuAsync(sku)).ReturnsAsync((Product?)null);
        _mockMapper.Setup(m => m.Map<Product>(dto)).Returns(product);
        _mockProductRepository.Setup(r => r.AddAsync(product)).ReturnsAsync(product);
        _mockMapper.Setup(m => m.Map<ProductDto>(product)).Returns(expectedDto);

        // Act
        var result = await _productService.CreateProductAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.SKU.Should().Be(sku);
    }

    [Fact]
    public async Task CreateProductAsync_WithVeryLongSku_CreatesProduct()
    {
        // Arrange
        var longSku = new string('A', 500); // Very long SKU
        var dto = new CreateUpdateProductDto(longSku, "Product");
        var product = new Product(Guid.NewGuid()) { SKU = longSku, Name = "Product" };
        var expectedDto = new ProductDto(Guid.NewGuid()) { SKU = longSku, Name = "Product" };

        _mockProductRepository.Setup(r => r.GetBySkuAsync(longSku)).ReturnsAsync((Product?)null);
        _mockMapper.Setup(m => m.Map<Product>(dto)).Returns(product);
        _mockProductRepository.Setup(r => r.AddAsync(product)).ReturnsAsync(product);
        _mockMapper.Setup(m => m.Map<ProductDto>(product)).Returns(expectedDto);

        // Act
        var result = await _productService.CreateProductAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.SKU.Should().Be(longSku);
    }

    [Fact]
    public async Task UpdateProductAsync_WithSameSku_UpdatesSuccessfully()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var sku = "PRD-001";
        var existingProduct = new Product(productId) { SKU = sku, Name = "Old Name" };
        var updateDto = new CreateUpdateProductDto(sku, "New Name");
        var updatedProduct = new Product(productId) { SKU = sku, Name = "New Name" };
        var expectedDto = new ProductDto(Guid.NewGuid()) { SKU = sku, Name = "New Name" };

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(existingProduct);
        _mockMapper.Setup(m => m.Map(updateDto, existingProduct));
        _mockProductRepository.Setup(r => r.UpdateAsync(existingProduct)).ReturnsAsync(updatedProduct);
        _mockMapper.Setup(m => m.Map<ProductDto>(updatedProduct)).Returns(expectedDto);

        // Act
        var result = await _productService.UpdateProductAsync(productId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Name");
        _mockProductRepository.Verify(r => r.GetBySkuAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProductAsync_WithEmptyName_UpdatesSuccessfully()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = new Product(productId) { SKU = "PRD-001", Name = "Old Name" };
        var updateDto = new CreateUpdateProductDto("PRD-001", "");
        var updatedProduct = new Product(productId) { SKU = "PRD-001", Name = "" };
        var expectedDto = new ProductDto(Guid.NewGuid()) { SKU = "PRD-001", Name = "" };

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(existingProduct);
        _mockMapper.Setup(m => m.Map(updateDto, existingProduct));
        _mockProductRepository.Setup(r => r.UpdateAsync(existingProduct)).ReturnsAsync(updatedProduct);
        _mockMapper.Setup(m => m.Map<ProductDto>(updatedProduct)).Returns(expectedDto);

        // Act
        var result = await _productService.UpdateProductAsync(productId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("");
    }

    [Fact]
    public async Task GetProductBySkuAsync_WithCaseInsensitiveSku_ReturnsProduct()
    {
        // Arrange
        var sku = "prd-001";
        var product = new Product(Guid.NewGuid()) { SKU = "PRD-001", Name = "Product" };
        var expectedDto = new ProductDto(Guid.NewGuid()) { SKU = "PRD-001", Name = "Product" };

        _mockProductRepository.Setup(r => r.GetBySkuAsync(sku)).ReturnsAsync(product);
        _mockMapper.Setup(m => m.Map<ProductDto>(product)).Returns(expectedDto);

        // Act
        var result = await _productService.GetProductBySkuAsync(sku);

        // Assert
        result.Should().NotBeNull();
        _mockProductRepository.Verify(r => r.GetBySkuAsync(sku), Times.Once);
    }

    [Fact]
    public async Task GetProductBySkuAsync_WithEmptySku_ReturnsNull()
    {
        // Arrange
        var sku = "";

        _mockProductRepository.Setup(r => r.GetBySkuAsync(sku)).ReturnsAsync((Product?)null);

        // Act
        var result = await _productService.GetProductBySkuAsync(sku);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllProductsAsync_WithEmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        _mockProductRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(Enumerable.Empty<Product>());
        _mockMapper.Setup(m => m.Map<IEnumerable<ProductDto>>(It.IsAny<IEnumerable<Product>>())).Returns(Enumerable.Empty<ProductDto>());

        // Act
        var result = await _productService.GetAllProductsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLowStockProductsAsync_WithNoLowStockProducts_ReturnsEmptyList()
    {
        // Arrange
        _mockProductRepository.Setup(r => r.GetLowStockProductsAsync()).ReturnsAsync(Enumerable.Empty<Product>());
        _mockMapper.Setup(m => m.Map<IEnumerable<ProductDto>>(It.IsAny<IEnumerable<Product>>())).Returns(Enumerable.Empty<ProductDto>());

        // Act
        var result = await _productService.GetLowStockProductsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteProductAsync_WithProductHavingDependencies_DeletesProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product(productId) { SKU = "PRD-001", Name = "Product" };

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _mockProductRepository.Setup(r => r.DeleteAsync(product)).Returns(Task.CompletedTask);

        // Act
        await _productService.DeleteProductAsync(productId);

        // Assert
        _mockProductRepository.Verify(r => r.DeleteAsync(product), Times.Once);
    }

    [Fact]
    public async Task CreateProductAsync_WhenRepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var dto = new CreateUpdateProductDto("PRD-001", "Product");
        var product = new Product(Guid.NewGuid()) { SKU = "PRD-001", Name = "Product" };

        _mockProductRepository.Setup(r => r.GetBySkuAsync(dto.SKU)).ReturnsAsync((Product?)null);
        _mockMapper.Setup(m => m.Map<Product>(dto)).Returns(product);
        _mockProductRepository.Setup(r => r.AddAsync(product)).ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _productService.CreateProductAsync(dto));
    }

    [Fact]
    public async Task UpdateProductAsync_WhenMapperThrowsException_PropagatesException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = new Product(productId) { SKU = "PRD-001", Name = "Old Name" };
        var updateDto = new CreateUpdateProductDto("PRD-001", "New Name");

        _mockProductRepository.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(existingProduct);
        _mockMapper.Setup(m => m.Map(updateDto, existingProduct)).Throws(new Exception("Mapping error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _productService.UpdateProductAsync(productId, updateDto));
    }

    [Fact]
    public async Task QueryProductsAsync_WithPagination_ReturnsPaginatedResult()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product(Guid.NewGuid()) { SKU = "PRD-001", Name = "Product 1" },
            new Product(Guid.NewGuid()) { SKU = "PRD-002", Name = "Product 2" }
        };

        var querySpec = new QuerySpec { Page = 1, PageSize = 2 };
        var spec = new ProductQuerySpec(querySpec);
        var paginatedResult = new PaginatedResult<Product>(products, 1, 2, 10);

        _mockProductRepository.Setup(r => r.QueryAsync(spec)).ReturnsAsync(paginatedResult);
        _mockMapper.Setup(m => m.Map<ProductDto>(It.IsAny<Product>())).Returns((Product p) =>
            new ProductDto(p.Id) { SKU = p.SKU, Name = p.Name });

        // Act
        var result = await _productService.QueryProductsAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalCount.Should().Be(10);
    }

    #endregion
}
