namespace LedgerMock.Domain;

public record TransferCompleted(
    string EventId,
    string SourceAccountId,
    string DestinationAccountId,
    long AmountInCents,
    DateTimeOffset Timestamp
) : IDomainEvent;
