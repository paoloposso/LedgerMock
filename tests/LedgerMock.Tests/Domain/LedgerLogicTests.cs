using LedgerMock.Domain;

namespace LedgerMock.Tests.Domain;

public class LedgerLogicTests
{
    [Fact]
    public void CalculateBalance_WithEmptyHistory_ReturnsZeroBalance()
    {
        // Arrange
        const string accountId = "acc-001";
        var history = Enumerable.Empty<Transaction>();

        // Act
        var result = LedgerLogic.CalculateBalance(accountId, history);

        // Assert
        Assert.Equal(accountId, result.AccountId);
        Assert.Equal(0L, result.BalanceInCents);
    }

    [Fact]
    public void CalculateBalance_WithDepositsAndWithdrawals_CalculatesCorrectBalance()
    {
        // Arrange
        const string accountId = "acc-001";
        var now = DateTimeOffset.UtcNow;
        Transaction[] history =
        [
            new(accountId, 10050L, TransactionType.Deposit, now.AddMinutes(-10), "evt-1"),
            new(accountId, 5025L, TransactionType.Withdrawal, now.AddMinutes(-5), "evt-2"),
            new(accountId, 2000L, TransactionType.Deposit, now.AddMinutes(-1), "evt-3")
        ];

        // Act
        var result = LedgerLogic.CalculateBalance(accountId, history);

        // Assert
        Assert.Equal(accountId, result.AccountId);
        Assert.Equal(7025L, result.BalanceInCents);
    }

    [Fact]
    public void CalculateBalance_WithTransferInAndTransferOut_AppliesCorrectDeltas()
    {
        // Arrange
        const string accountId = "acc-002";
        var now = DateTimeOffset.UtcNow;
        Transaction[] history =
        [
            new(accountId, 30000L, TransactionType.TransferIn, now.AddMinutes(-3), "evt-1"),
            new(accountId, 12000L, TransactionType.TransferOut, now.AddMinutes(-2), "evt-2")
        ];

        // Act
        var result = LedgerLogic.CalculateBalance(accountId, history);

        // Assert
        Assert.Equal(18000L, result.BalanceInCents);
    }

    [Theory]
    [InlineData(TransactionType.Deposit, 5000L, 15000L)]
    [InlineData(TransactionType.TransferIn, 5000L, 15000L)]
    [InlineData(TransactionType.Withdrawal, 5000L, 5000L)]
    [InlineData(TransactionType.TransferOut, 5000L, 5000L)]
    public void ApplyTransaction_CalculatesExpectedState(TransactionType type, long amountInCents, long expectedBalanceInCents)
    {
        // Arrange
        var current = new AccountState("acc-003", 10000L);
        var tx = new Transaction("acc-003", amountInCents, type, DateTimeOffset.UtcNow, "evt-x");

        // Act
        var result = LedgerLogic.ApplyTransaction(current, tx);

        // Assert
        Assert.Equal(expectedBalanceInCents, result.BalanceInCents);
    }

    [Fact]
    public void ApplyTransaction_WithUnknownType_ThrowsArgumentException()
    {
        // Arrange
        var current = new AccountState("acc-004", 10000L);
        var tx = new Transaction("acc-004", 5000L, (TransactionType)999, DateTimeOffset.UtcNow, "evt-err");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => LedgerLogic.ApplyTransaction(current, tx));
    }
}
