namespace LedgerMock.Domain;

public record AccountState(
    string AccountId,
    long BalanceInCents
);
