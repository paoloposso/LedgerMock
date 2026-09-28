using System;

namespace LedgerMock.Domain;

public record DomainEvent(
    string EventId,
    string EventType,
    object Payload,
    DateTimeOffset Timestamp
);
