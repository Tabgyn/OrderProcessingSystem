using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Domain;

namespace PaymentService.Infrastructure;

public interface IPaymentRepository
{
    Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<Payment> CreatePaymentAsync(Payment payment, CancellationToken cancellationToken = default);
    Task UpdatePaymentAsync(Payment payment, CancellationToken cancellationToken = default);
}

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
    }

    public async Task<Payment> CreatePaymentAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await _context.Payments.AddAsync(payment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public async Task UpdatePaymentAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync(cancellationToken);
    }
}