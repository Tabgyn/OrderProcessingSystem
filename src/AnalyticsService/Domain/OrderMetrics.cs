namespace AnalyticsService.Domain;

public class OrderMetrics
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public int TotalOrders { get; set; }
    public int ConfirmedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public int FailedOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public DateTime UpdatedAt { get; set; }
}