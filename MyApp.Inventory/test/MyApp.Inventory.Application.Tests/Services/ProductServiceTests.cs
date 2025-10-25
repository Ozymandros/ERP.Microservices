using AutoMapper;
using Moq;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Services;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;
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
        var product = new Product { Id = productId, SKU = "PRD-001", Name = "Test Product" };
        var expectedDto = new ProductDto { SKU = "PRD-001", Name = "Test Product" };

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
        var product = new Product { SKU = sku, Name = "Test Product" };
        var expectedDto = new ProductDto { SKU = sku, Name = "Test Product" };

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

    #region GetAllProductsAsync Tests

    [Fact]
    public async Task GetAllProductsAsync_ReturnsAllProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product { SKU = "PRD-001", Name = "Product 1" },
            new Product { SKU = "PRD-002", Name = "Product 2" }
        };

        var productDtos = new List<ProductDto>
        {
            new ProductDto { SKU = "PRD-001", Name = "Product 1" },
            new ProductDto { SKU = "PRD-002", Name = "Product 2" }
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
            new Product { SKU = "PRD-LOW", Name = "Low Stock Product" }
        };

        var productDtos = new List<ProductDto>
        {
            new ProductDto { SKU = "PRD-LOW", Name = "Low Stock Product" }
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
        var dto = new CreateUpdateProductDto { SKU = "PRD-NEW", Name = "New Product" };
        var product = new Product { SKU = "PRD-NEW", Name = "New Product" };
        var createdProduct = new Product { Id = Guid.NewGuid(), SKU = "PRD-NEW", Name = "New Product" };
        var expectedDto = new ProductDto { SKU = "PRD-NEW", Name = "New Product" };

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
        var dto = new CreateUpdateProductDto { SKU = "PRD-DUPLICATE", Name = "Product" };
        var existingProduct = new Product { SKU = "PRD-DUPLICATE" };

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
        var existingProduct = new Product { Id = productId, SKU = "PRD-OLD", Name = "Old Name" };
        var updateDto = new CreateUpdateProductDto { SKU = "PRD-OLD", Name = "New Name" };
        var updatedProduct = new Product { Id = productId, SKU = "PRD-OLD", Name = "New Name" };
        var expectedDto = new ProductDto { SKU = "PRD-OLD", Name = "New Name" };

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
        var updateDto = new CreateUpdateProductDto { SKU = "PRD-001", Name = "Product" };

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
        var existingProduct = new Product { Id = productId, SKU = "PRD-OLD" };
        var updateDto = new CreateUpdateProductDto { SKU = "PRD-NEW", Name = "Product" };
        var conflictingProduct = new Product { Id = Guid.NewGuid(), SKU = "PRD-NEW" };

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
        var product = new Product { Id = productId, SKU = "PRD-001" };

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
}
