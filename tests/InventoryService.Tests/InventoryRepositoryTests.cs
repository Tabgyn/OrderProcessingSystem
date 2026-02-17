using InventoryService.Data;
using InventoryService.Domain;
using InventoryService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace InventoryService.Tests;

public class InventoryRepositoryTests
{
    private InventoryDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new InventoryDbContext(options);

        context.Products.AddRange(
            new Product
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Laptop",
                Sku = "LAP-001",
                AvailableQuantity = 50,
                ReservedQuantity = 0,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Mouse",
                Sku = "MOU-001",
                AvailableQuantity = 5,
                ReservedQuantity = 0,
                CreatedAt = DateTime.UtcNow
            }
        );

        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task GetProductByIdAsync_ExistingProduct_ReturnsProduct()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<InventoryRepository>>();
        var repository = new InventoryRepository(context, logger.Object);
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act
        var product = await repository.GetProductByIdAsync(productId);

        // Assert
        Assert.NotNull(product);
        Assert.Equal("Laptop", product.Name);
        Assert.Equal(50, product.AvailableQuantity);
    }

    [Fact]
    public async Task GetProductByIdAsync_NonExistingProduct_ReturnsNull()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<InventoryRepository>>();
        var repository = new InventoryRepository(context, logger.Object);

        // Act
        var product = await repository.GetProductByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(product);
    }

    [Fact]
    public async Task ReserveInventoryAsync_SufficientStock_ReturnsTrue()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<InventoryRepository>>();
        var repository = new InventoryRepository(context, logger.Object);
        var orderId = Guid.NewGuid();
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act
        var result = await repository.ReserveInventoryAsync(
            orderId,
            new List<(Guid, int)> { (productId, 5) });

        // Assert
        Assert.True(result);

        var product = await repository.GetProductByIdAsync(productId);
        Assert.NotNull(product);
        Assert.Equal(45, product.AvailableQuantity);
        Assert.Equal(5, product.ReservedQuantity);
    }

    [Fact]
    public async Task ReserveInventoryAsync_InsufficientStock_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<InventoryRepository>>();
        var repository = new InventoryRepository(context, logger.Object);
        var orderId = Guid.NewGuid();
        var productId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Act - request more than available (5)
        var result = await repository.ReserveInventoryAsync(
            orderId,
            new List<(Guid, int)> { (productId, 100) });

        // Assert
        Assert.False(result);

        var product = await repository.GetProductByIdAsync(productId);
        Assert.NotNull(product);
        Assert.Equal(5, product.AvailableQuantity);
        Assert.Equal(0, product.ReservedQuantity);
    }

    [Fact]
    public async Task ReserveInventoryAsync_NonExistingProduct_ReturnsFalse()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<InventoryRepository>>();
        var repository = new InventoryRepository(context, logger.Object);

        // Act
        var result = await repository.ReserveInventoryAsync(
            Guid.NewGuid(),
            new List<(Guid, int)> { (Guid.NewGuid(), 1) });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ReleaseReservationAsync_ActiveReservation_ReleasesInventory()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var logger = new Mock<ILogger<InventoryRepository>>();
        var repository = new InventoryRepository(context, logger.Object);
        var orderId = Guid.NewGuid();
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Reserve first
        await repository.ReserveInventoryAsync(
            orderId,
            new List<(Guid, int)> { (productId, 10) });

        // Act
        await repository.ReleaseReservationAsync(orderId);

        // Assert
        var product = await repository.GetProductByIdAsync(productId);
        Assert.NotNull(product);
        Assert.Equal(50, product.AvailableQuantity);
        Assert.Equal(0, product.ReservedQuantity);
    }
}