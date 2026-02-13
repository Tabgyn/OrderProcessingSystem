using Microsoft.EntityFrameworkCore;
using AnalyticsService.Data;
using AnalyticsService.Domain;

namespace AnalyticsService.Infrastructure;

public interface IAnalyticsRepository
{
    Task LogEventAsync(EventLog eventLog, CancellationToken cancellationToken = default);
    Task<OrderMetrics?> GetMetricsForDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task UpdateMetricsAsync(OrderMetrics metrics, CancellationToken cancellationToken = default);
    Task<List<OrderMetrics>> GetMetricsRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<List<EventLog>> GetRecentEventsAsync(int count, CancellationToken cancellationToken = default);
}

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly AnalyticsDbContext _context;

    public AnalyticsRepository(AnalyticsDbContext context)
    {
        _context = context;
    }

    public async Task LogEventAsync(EventLog eventLog, CancellationToken cancellationToken = default)
    {
        await _context.EventLogs.AddAsync(eventLog, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<OrderMetrics?> GetMetricsForDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var dateOnly = date.Date;
        return await _context.OrderMetrics
            .FirstOrDefaultAsync(m => m.Date == dateOnly, cancellationToken);
    }

    public async Task UpdateMetricsAsync(OrderMetrics metrics, CancellationToken cancellationToken = default)
    {
        var existing = await GetMetricsForDateAsync(metrics.Date, cancellationToken);

        if (existing == null)
        {
            await _context.OrderMetrics.AddAsync(metrics, cancellationToken);
        }
        else
        {
            existing.TotalOrders = metrics.TotalOrders;
            existing.ConfirmedOrders = metrics.ConfirmedOrders;
            existing.CancelledOrders = metrics.CancelledOrders;
            existing.FailedOrders = metrics.FailedOrders;
            existing.TotalRevenue = metrics.TotalRevenue;
            existing.UpdatedAt = DateTime.UtcNow;
            _context.OrderMetrics.Update(existing);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<OrderMetrics>> GetMetricsRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.OrderMetrics
            .Where(m => m.Date >= startDate.Date && m.Date <= endDate.Date)
            .OrderBy(m => m.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<EventLog>> GetRecentEventsAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _context.EventLogs
            .OrderByDescending(e => e.ReceivedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}