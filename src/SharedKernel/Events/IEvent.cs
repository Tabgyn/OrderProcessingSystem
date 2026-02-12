namespace SharedKernel.Events;

public interface IEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
    string EventType { get; }
}

public abstract class BaseEvent : IEvent
{
    public Guid EventId { get; protected set; } = Guid.NewGuid();
    public DateTime OccurredAt { get; protected set; } = DateTime.UtcNow;
    public abstract string EventType { get; }
}