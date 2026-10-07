using System.Collections.Generic;
using System.Threading.Tasks;

namespace LedgerMock.Domain;

public interface ILedgerStore
{
    Task<AppendResult> AppendTransactionAsync(Transaction tx, string idempotencyKey);
    Task<AppendResult> AppendTransferAsync(Transaction debitTx, Transaction creditTx, string idempotencyKey);
    Task<List<Transaction>> GetEventsAsync(string accountId);
}
