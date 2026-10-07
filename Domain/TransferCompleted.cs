namespace LedgerMock.Domain;

public record TransferCompleted(
    string EventId,
    string SourceAccountId,
    string DestinationAccountId,
    decimal Amount,
    DateTimeOffset Timestamp
) : IDomainEvent;
