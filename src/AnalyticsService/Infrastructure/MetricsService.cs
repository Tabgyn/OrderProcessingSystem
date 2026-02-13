using AnalyticsService.Domain;

namespace AnalyticsService.Infrastructure;

public interface IMetricsService
{
    Task IncrementOrderPlacedAsync(decimal amount, CancellationToken cancellationToken = default);
    Task IncrementOrderConfirmedAsync(CancellationToken cancellationToken = default);
    Task IncrementOrderCancelledAsync(CancellationToken cancellationToken = default);
    Task IncrementOrderFailedAsync(CancellationToken cancellationToken = default);
}

public class MetricsService : IMetricsService
{
    private readonly IAnalyticsRepository _repository;
    private readonly ILogger<MetricsService> _logger;

    public MetricsService(IAnalyticsRepository repository, ILogger<MetricsService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task IncrementOrderPlacedAsync(decimal amount, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var metrics = await _repository.GetMetricsForDateAsync(today, cancellationToken)
            ?? new OrderMetrics { Id = Guid.NewGuid(), Date = today };

        metrics.TotalOrders++;
        metrics.TotalRevenue += amount;
        metrics.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateMetricsAsync(metrics, cancellationToken);

        _logger.LogInformation("📊 Metrics updated: TotalOrders={Total}, Revenue=${Revenue:F2}",
            metrics.TotalOrders, metrics.TotalRevenue);
    }

    public async Task IncrementOrderConfirmedAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var metrics = await _repository.GetMetricsForDateAsync(today, cancellationToken);

        if (metrics != null)
        {
            metrics.ConfirmedOrders++;
            metrics.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateMetricsAsync(metrics, cancellationToken);

            _logger.LogInformation("📊 Confirmed orders: {Count}", metrics.ConfirmedOrders);
        }
    }

    public async Task IncrementOrderCancelledAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var metrics = await _repository.GetMetricsForDateAsync(today, cancellationToken);

        if (metrics != null)
        {
            metrics.CancelledOrders++;
            metrics.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateMetricsAsync(metrics, cancellationToken);

            _logger.LogInformation("📊 Cancelled orders: {Count}", metrics.CancelledOrders);
        }
    }

    public async Task IncrementOrderFailedAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var metrics = await _repository.GetMetricsForDateAsync(today, cancellationToken);

        if (metrics != null)
        {
            metrics.FailedOrders++;
            metrics.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateMetricsAsync(metrics, cancellationToken);

            _logger.LogInformation("📊 Failed orders: {Count}", metrics.FailedOrders);
        }
    }
}