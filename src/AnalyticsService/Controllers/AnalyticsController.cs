using Microsoft.AspNetCore.Mvc;
using AnalyticsService.Infrastructure;

namespace AnalyticsService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsRepository _repository;

    public AnalyticsController(IAnalyticsRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("metrics/today")]
    public async Task<IActionResult> GetTodayMetrics(CancellationToken cancellationToken)
    {
        var metrics = await _repository.GetMetricsForDateAsync(DateTime.UtcNow.Date, cancellationToken);
        if (metrics == null)
        {
            return NotFound(new { message = "No metrics available for today" });
        }

        return Ok(metrics);
    }

    [HttpGet("metrics/range")]
    public async Task<IActionResult> GetMetricsRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        var metrics = await _repository.GetMetricsRangeAsync(startDate, endDate, cancellationToken);
        return Ok(metrics);
    }

    [HttpGet("events/recent")]
    public async Task<IActionResult> GetRecentEvents(
        [FromQuery] int count = 50,
        CancellationToken cancellationToken = default)
    {
        var events = await _repository.GetRecentEventsAsync(count, cancellationToken);
        return Ok(events);
    }
}