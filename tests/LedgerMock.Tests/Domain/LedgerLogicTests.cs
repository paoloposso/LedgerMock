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
        Assert.Equal(0m, result.Balance);
    }

    [Fact]
    public void CalculateBalance_WithDepositsAndWithdrawals_CalculatesCorrectBalance()
    {
        // Arrange
        const string accountId = "acc-001";
        var now = DateTimeOffset.UtcNow;
        Transaction[] history =
        [
            new(accountId, 100.50m, TransactionType.Deposit, now.AddMinutes(-10), "evt-1"),
            new(accountId, 50.25m, TransactionType.Withdrawal, now.AddMinutes(-5), "evt-2"),
            new(accountId, 20.00m, TransactionType.Deposit, now.AddMinutes(-1), "evt-3")
        ];

        // Act
        var result = LedgerLogic.CalculateBalance(accountId, history);

        // Assert
        Assert.Equal(accountId, result.AccountId);
        Assert.Equal(70.25m, result.Balance);
    }

    [Fact]
    public void CalculateBalance_WithTransferInAndTransferOut_AppliesCorrectDeltas()
    {
        // Arrange
        const string accountId = "acc-002";
        var now = DateTimeOffset.UtcNow;
        Transaction[] history =
        [
            new(accountId, 300.00m, TransactionType.TransferIn, now.AddMinutes(-3), "evt-1"),
            new(accountId, 120.00m, TransactionType.TransferOut, now.AddMinutes(-2), "evt-2")
        ];

        // Act
        var result = LedgerLogic.CalculateBalance(accountId, history);

        // Assert
        Assert.Equal(180.00m, result.Balance);
    }

    [Theory]
    [InlineData(TransactionType.Deposit, 50, 150)]
    [InlineData(TransactionType.TransferIn, 50, 150)]
    [InlineData(TransactionType.Withdrawal, 50, 50)]
    [InlineData(TransactionType.TransferOut, 50, 50)]
    public void ApplyTransaction_CalculatesExpectedState(TransactionType type, decimal amount, decimal expectedBalance)
    {
        // Arrange
        var current = new AccountState("acc-003", 100m);
        var tx = new Transaction("acc-003", amount, type, DateTimeOffset.UtcNow, "evt-x");

        // Act
        var result = LedgerLogic.ApplyTransaction(current, tx);

        // Assert
        Assert.Equal(expectedBalance, result.Balance);
    }

    [Fact]
    public void ApplyTransaction_WithUnknownType_ThrowsArgumentException()
    {
        // Arrange
        var current = new AccountState("acc-004", 100m);
        var tx = new Transaction("acc-004", 50m, (TransactionType)999, DateTimeOffset.UtcNow, "evt-err");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => LedgerLogic.ApplyTransaction(current, tx));
    }
}
