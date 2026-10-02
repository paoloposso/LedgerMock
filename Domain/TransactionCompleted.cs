namespace LedgerMock.Domain;

public record TransactionCompleted(
    string EventId,
    Transaction Transaction,
    DateTimeOffset Timestamp
) : IDomainEvent;
