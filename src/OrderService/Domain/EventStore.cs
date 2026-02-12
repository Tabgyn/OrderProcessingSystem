namespace OrderService.Domain;

public class EventStore
{
    public long Id { get; set; }
    public Guid AggregateId { get; set; } // OrderId
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty; // JSON serialized event
    public int Version { get; set; }
    public DateTime OccurredAt { get; set; }
}