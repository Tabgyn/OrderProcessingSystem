using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Domain;
using SharedKernel.Events;

namespace OrderService.Infrastructure;

public interface IEventStoreRepository
{
    Task SaveEventAsync<TEvent>(Guid aggregateId, TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent;
    Task<List<IEvent>> GetEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default);
}

public class EventStoreRepository : IEventStoreRepository
{
    private readonly OrderDbContext _context;
    private readonly ILogger<EventStoreRepository> _logger;

    public EventStoreRepository(OrderDbContext context, ILogger<EventStoreRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SaveEventAsync<TEvent>(Guid aggregateId, TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        var version = await GetNextVersionAsync(aggregateId, cancellationToken);

        var eventStore = new EventStore
        {
            AggregateId = aggregateId,
            EventType = @event.EventType,
            EventData = JsonSerializer.Serialize(@event),
            Version = version,
            OccurredAt = @event.OccurredAt
        };

        await _context.EventStore.AddAsync(eventStore, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Event {EventType} saved for aggregate {AggregateId} at version {Version}",
            @event.EventType, aggregateId, version);
    }

    public async Task<List<IEvent>> GetEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        var eventStores = await _context.EventStore
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.Version)
            .ToListAsync(cancellationToken);

        var events = new List<IEvent>();

        foreach (var eventStore in eventStores)
        {
            var eventType = Type.GetType($"SharedKernel.Events.{eventStore.EventType}, SharedKernel");
            if (eventType != null)
            {
                var @event = JsonSerializer.Deserialize(eventStore.EventData, eventType) as IEvent;
                if (@event != null)
                {
                    events.Add(@event);
                }
            }
        }

        return events;
    }

    private async Task<int> GetNextVersionAsync(Guid aggregateId, CancellationToken cancellationToken)
    {
        var maxVersion = await _context.EventStore
            .Where(e => e.AggregateId == aggregateId)
            .MaxAsync(e => (int?)e.Version, cancellationToken);

        return (maxVersion ?? 0) + 1;
    }
}