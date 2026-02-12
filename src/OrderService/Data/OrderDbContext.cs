using Microsoft.EntityFrameworkCore;
using OrderService.Domain;

namespace OrderService.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<EventStore> EventStore => Set<EventStore>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerId).IsRequired();
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.Status).IsRequired();
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Event Store configuration
        modelBuilder.Entity<EventStore>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AggregateId).IsRequired();
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(200);
            entity.Property(e => e.EventData).IsRequired();
            entity.Property(e => e.OccurredAt).IsRequired();
            entity.HasIndex(e => e.AggregateId);
            entity.HasIndex(e => e.OccurredAt);
            entity.HasIndex(e => new { e.AggregateId, e.Version }).IsUnique();
        });
    }
}