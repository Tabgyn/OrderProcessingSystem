using Microsoft.EntityFrameworkCore;
using InventoryService.Data;
using InventoryService.Domain;

namespace InventoryService.Infrastructure;

public interface IInventoryRepository
{
    Task<Product?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<List<Product>> GetProductsByIdsAsync(List<Guid> productIds, CancellationToken cancellationToken = default);
    Task<bool> ReserveInventoryAsync(Guid orderId, List<(Guid ProductId, int Quantity)> items, CancellationToken cancellationToken = default);
    Task ReleaseReservationAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task UpdateProductAsync(Product product, CancellationToken cancellationToken = default);
}

public class InventoryRepository : IInventoryRepository
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<InventoryRepository> _logger;

    public InventoryRepository(InventoryDbContext context, ILogger<InventoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Product?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.Products.FindAsync(new object[] { productId }, cancellationToken);
    }

    public async Task<List<Product>> GetProductsByIdsAsync(List<Guid> productIds, CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ReserveInventoryAsync(Guid orderId, List<(Guid ProductId, int Quantity)> items, CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var productIds = items.Select(i => i.ProductId).ToList();
            var products = await GetProductsByIdsAsync(productIds, cancellationToken);

            // Check if all products exist and have sufficient inventory
            foreach (var item in items)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product == null)
                {
                    _logger.LogWarning("Product {ProductId} not found", item.ProductId);
                    return false;
                }

                if (product.AvailableQuantity < item.Quantity)
                {
                    _logger.LogWarning(
                        "Insufficient inventory for product {ProductId}. Available: {Available}, Requested: {Requested}",
                        item.ProductId, product.AvailableQuantity, item.Quantity);
                    return false;
                }
            }

            // Reserve inventory
            var reservation = new InventoryReservation
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                ReservedAt = DateTime.UtcNow,
                IsActive = true,
                Items = items.Select(i => new ReservationItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList()
            };

            await _context.Reservations.AddAsync(reservation, cancellationToken);

            // Update product quantities
            foreach (var item in items)
            {
                var product = products.First(p => p.Id == item.ProductId);
                product.AvailableQuantity -= item.Quantity;
                product.ReservedQuantity += item.Quantity;
                product.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Inventory reserved for order {OrderId}", orderId);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error reserving inventory for order {OrderId}", orderId);
            return false;
        }
    }

    public async Task ReleaseReservationAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.OrderId == orderId && r.IsActive, cancellationToken);

        if (reservation == null)
        {
            _logger.LogWarning("No active reservation found for order {OrderId}", orderId);
            return;
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var productIds = reservation.Items.Select(i => i.ProductId).ToList();
            var products = await GetProductsByIdsAsync(productIds, cancellationToken);

            foreach (var item in reservation.Items)
            {
                var product = products.First(p => p.Id == item.ProductId);
                product.AvailableQuantity += item.Quantity;
                product.ReservedQuantity -= item.Quantity;
                product.UpdatedAt = DateTime.UtcNow;
            }

            reservation.IsActive = false;
            reservation.ReleasedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Inventory released for order {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error releasing inventory for order {OrderId}", orderId);
            throw;
        }
    }

    public async Task UpdateProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        product.UpdatedAt = DateTime.UtcNow;
        _context.Products.Update(product);
        await _context.SaveChangesAsync(cancellationToken);
    }
}