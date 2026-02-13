using Microsoft.EntityFrameworkCore;
using AnalyticsService.Domain;

namespace AnalyticsService.Data;

public class AnalyticsDbContext : DbContext
{
    public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options) : base(options)
    {
    }

    public DbSet<EventLog> EventLogs => Set<EventLog>();
    public DbSet<OrderMetrics> OrderMetrics => Set<OrderMetrics>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EventLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventId).IsRequired();
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(200);
            entity.Property(e => e.EventData).IsRequired();
            entity.HasIndex(e => e.EventId);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.OccurredAt);
            entity.HasIndex(e => e.ReceivedAt);
        });

        modelBuilder.Entity<OrderMetrics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.TotalRevenue).HasPrecision(18, 2);
            entity.HasIndex(e => e.Date).IsUnique();
        });
    }
}