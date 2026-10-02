using System.Collections.Generic;
using System.Threading.Tasks;

namespace LedgerMock.Domain;

public interface ILedgerStore
{
    Task<AppendResult> AppendTransactionAsync(Transaction tx, string idempotencyKey);
    Task<List<Transaction>> GetEventsAsync(string accountId);
}
