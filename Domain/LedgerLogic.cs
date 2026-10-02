namespace LedgerMock.Domain;

public static class LedgerLogic
{
    // Pure function: Given a starting state and a new transaction, return the new state
    public static AccountState ApplyTransaction(AccountState current, Transaction transaction)
    {
        var newBalance = transaction.Type switch
        {
            TransactionType.Deposit => current.Balance + transaction.Amount,
            TransactionType.TransferIn => current.Balance + transaction.Amount,
            TransactionType.Withdrawal => current.Balance - transaction.Amount,
            TransactionType.TransferOut => current.Balance - transaction.Amount,
            _ => throw new ArgumentException("Unknown transaction type")
        };

        return current with { Balance = newBalance };
    }

    // Pure function: Reduce a list of events into a final balance
    public static AccountState CalculateBalance(string accountId, IEnumerable<Transaction> history)
    {
        var initialState = new AccountState(accountId, 0m);
        return history.Aggregate(initialState, ApplyTransaction);
    }
}
