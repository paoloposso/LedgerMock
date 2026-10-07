using LedgerMock.Domain;
using LedgerMock.Infrastructure;

namespace LedgerMock;

public static class Endpoints
{
    public static void MapLedgerEndpoints(WebApplication app)
    {
        // 1. POST: Deposit funds to an account
        app.MapPost("/accounts/{id}/deposit", async (string id, DepositRequest request, ILedgerStore store, EventBus eventBus) =>
        {
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                return Results.BadRequest(new { error = "IdempotencyKey is strictly required." });
            }

            if (request.AmountInCents <= 0)
            {
                return Results.BadRequest(new { error = "AmountInCents must be greater than zero." });
            }

            var tx = new Transaction(
                id,
                request.AmountInCents,
                TransactionType.Deposit,
                DateTimeOffset.UtcNow,
                request.IdempotencyKey
            );

            var result = await store.AppendTransactionAsync(tx, request.IdempotencyKey);
            if (result == AppendResult.Duplicate)
            {
                return Results.Ok(new { message = "Deposit already processed (Idempotent response)." });
            }

            await eventBus.PublishAsync(new TransactionCompleted(Guid.NewGuid().ToString(), tx, DateTimeOffset.UtcNow));
            return Results.Ok(new { message = "Deposit processed successfully." });
        });

        // 2. POST: Withdraw funds from an account
        app.MapPost("/accounts/{id}/withdraw", async (string id, WithdrawRequest request, ILedgerStore store, EventBus eventBus) =>
        {
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                return Results.BadRequest(new { error = "IdempotencyKey is strictly required." });
            }

            if (request.AmountInCents <= 0)
            {
                return Results.BadRequest(new { error = "AmountInCents must be greater than zero." });
            }

            var tx = new Transaction(
                id,
                request.AmountInCents,
                TransactionType.Withdrawal,
                DateTimeOffset.UtcNow,
                request.IdempotencyKey
            );

            var result = await store.AppendTransactionAsync(tx, request.IdempotencyKey);
            if (result == AppendResult.Duplicate)
            {
                return Results.Ok(new { message = "Withdrawal already processed (Idempotent response)." });
            }

            await eventBus.PublishAsync(new TransactionCompleted(Guid.NewGuid().ToString(), tx, DateTimeOffset.UtcNow));
            return Results.Ok(new { message = "Withdrawal processed successfully." });
        });

        // 3. POST: Atomic two-legged Transfer between two accounts
        app.MapPost("/transfers", async (TransferRequest request, ILedgerStore store, EventBus eventBus) =>
        {
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                return Results.BadRequest(new { error = "IdempotencyKey is strictly required." });
            }

            if (request.AmountInCents <= 0)
            {
                return Results.BadRequest(new { error = "AmountInCents must be greater than zero." });
            }

            if (request.SourceAccountId == request.DestinationAccountId)
            {
                return Results.BadRequest(new { error = "Source and destination accounts must be different." });
            }

            var timestamp = DateTimeOffset.UtcNow;
            var debitTx = new Transaction(
                request.SourceAccountId,
                request.AmountInCents,
                TransactionType.TransferOut,
                timestamp,
                $"{request.IdempotencyKey}-debit"
            );

            var creditTx = new Transaction(
                request.DestinationAccountId,
                request.AmountInCents,
                TransactionType.TransferIn,
                timestamp,
                $"{request.IdempotencyKey}-credit"
            );

            // ACID append: Debit leg + Credit leg + Idempotency lock committed simultaneously
            var result = await store.AppendTransferAsync(debitTx, creditTx, request.IdempotencyKey);
            if (result == AppendResult.Duplicate)
            {
                return Results.Ok(new { message = "Transfer already processed (Idempotent response)." });
            }

            await eventBus.PublishAsync(new TransferCompleted(
                Guid.NewGuid().ToString(),
                request.SourceAccountId,
                request.DestinationAccountId,
                request.AmountInCents,
                timestamp
            ));

            return Results.Ok(new { message = "Transfer processed atomically." });
        });

        // 4. GET: Calculate account balance via Event Sourcing fold
        app.MapGet("/accounts/{id}/balance", async (string id, ILedgerStore store) =>
        {
            var history = await store.GetEventsAsync(id);
            var state = LedgerLogic.CalculateBalance(id, history);
            return Results.Ok(state);
        });
    }
}

public record DepositRequest(long AmountInCents, string IdempotencyKey);
public record WithdrawRequest(long AmountInCents, string IdempotencyKey);
public record TransferRequest(string SourceAccountId, string DestinationAccountId, long AmountInCents, string IdempotencyKey);
