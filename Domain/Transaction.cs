namespace LedgerMock.Domain;

public enum TransactionType { 
    Deposit, 
    Withdrawal, 
    TransferIn, 
    TransferOut 
}

public record Transaction(
    string AccountId,
    long AmountInCents,
    TransactionType Type,
    DateTimeOffset Timestamp,
    string EventId
);
