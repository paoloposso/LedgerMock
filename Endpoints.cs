using LedgerMock.Domain;
using LedgerMock.Infrastructure;

namespace LedgerMock;

public static class Endpoints
{
    public static void MapLedgerEndpoints(WebApplication app)
    {
        // POST: Trigger a new transaction
        app.MapPost("/transfer", async (TransferRequest request, ILedgerStore store, EventBus eventBus) => 
        {
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                return Results.BadRequest("IdempotencyKey is strictly required.");
            }

            var newFact = new Transaction(
                request.AccountId, 
                request.Amount, 
                Enum.Parse<TransactionType>(request.Type, true), 
                DateTimeOffset.UtcNow, 
                request.IdempotencyKey 
            );

            // 1. I/O: Append to Store with strict Idempotency
            var appendResult = await store.AppendTransactionAsync(newFact, request.IdempotencyKey);
            if (appendResult == AppendResult.Duplicate)
            {
                // If store rejected it because the idempotency key already exists, safely return early!
                return Results.Ok(new { message = "Transaction already processed successfully (Idempotent response)." });
            }

            // 2. Event-Driven: Broadcast to "Kafka"
            var domainEvent = new TransactionCompleted(
                Guid.NewGuid().ToString(), 
                newFact, 
                DateTimeOffset.UtcNow
            );
            await eventBus.PublishAsync(domainEvent);
            
            return Results.Ok(new { message = "Transaction processed!" });
        });

        // GET: Calculate final balance based on Event Sourcing
        app.MapGet("/account/{id}/balance", async (string id, ILedgerStore store) => 
        {
            var history = await store.GetEventsAsync(id);
            var state = LedgerLogic.CalculateBalance(id, history);
            return Results.Ok(state);
        });
    }
}

// Added IdempotencyKey to the request payload
public record TransferRequest(string AccountId, decimal Amount, string Type, string IdempotencyKey);
