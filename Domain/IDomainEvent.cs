using System;

namespace LedgerMock.Domain;

public interface IDomainEvent
{
    string EventId { get; }
    DateTimeOffset Timestamp { get; }
}
