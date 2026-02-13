using Microsoft.EntityFrameworkCore;
using PaymentService.Domain;

namespace PaymentService.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderId).IsRequired();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.TransactionId).HasMaxLength(100);
            entity.Property(e => e.ErrorCode).HasMaxLength(50);
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.TransactionId);
            entity.HasIndex(e => e.Status);
        });
    }
}