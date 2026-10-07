namespace LedgerMock.Domain;

public static class LedgerLogic
{
    // Pure function: Given a starting state and a new transaction, return the new state
    public static AccountState ApplyTransaction(AccountState current, Transaction transaction)
    {
        var newBalance = transaction.Type switch
        {
            TransactionType.Deposit => current.BalanceInCents + transaction.AmountInCents,
            TransactionType.TransferIn => current.BalanceInCents + transaction.AmountInCents,
            TransactionType.Withdrawal => current.BalanceInCents - transaction.AmountInCents,
            TransactionType.TransferOut => current.BalanceInCents - transaction.AmountInCents,
            _ => throw new ArgumentException("Unknown transaction type")
        };

        return current with { BalanceInCents = newBalance };
    }

    // Pure function: Reduce a list of events into a final balance
    public static AccountState CalculateBalance(string accountId, IEnumerable<Transaction> history)
    {
        var initialState = new AccountState(accountId, 0L);
        return history.Aggregate(initialState, ApplyTransaction);
    }
}
