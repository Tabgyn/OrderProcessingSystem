using Microsoft.EntityFrameworkCore;
using InventoryService.Domain;

namespace InventoryService.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryReservation> Reservations => Set<InventoryReservation>();
    public DbSet<ReservationItem> ReservationItems => Set<ReservationItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Sku).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Sku).IsUnique();
        });

        modelBuilder.Entity<InventoryReservation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderId).IsRequired();
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<ReservationItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne<InventoryReservation>()
                .WithMany(r => r.Items)
                .HasForeignKey(e => e.ReservationId);
        });

        // Seed sample products
        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Laptop",
                Sku = "LAP-001",
                AvailableQuantity = 50,
                ReservedQuantity = 0,
                CreatedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc)
            },
            new Product
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Mouse",
                Sku = "MOU-001",
                AvailableQuantity = 200,
                ReservedQuantity = 0,
                CreatedAt = new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc)
            },
            new Product
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Keyboard",
                Sku = "KEY-001",
                AvailableQuantity = 150,
                ReservedQuantity = 0,
                CreatedAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}