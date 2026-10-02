using System.Threading.Channels;
using LedgerMock.Domain;

namespace LedgerMock.Infrastructure;

// Simulates a Kafka Topic / Message Broker
// We use System.Threading.Channels for high-performance, concurrent, in-memory pub/sub.
public class EventBus
{
    private readonly Channel<IDomainEvent> _channel;

    public EventBus()
    {
        // Bounded channel to prevent out-of-memory exceptions if consumers are slow (backpressure handling)
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<IDomainEvent>(options);
    }

    // Producers (like our API) call this to publish events
    public async Task PublishAsync(IDomainEvent domainEvent)
    {
        await _channel.Writer.WriteAsync(domainEvent);
    }

    // Consumers (like our BackgroundService) read from this asynchronously
    public IAsyncEnumerable<IDomainEvent> SubscribeAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
