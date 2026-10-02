using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using LedgerMock.Domain;
using LedgerMock.Infrastructure;

namespace LedgerMock.Workers;

public class NotificationWorker(EventBus eventBus, ILogger<NotificationWorker> logger) : BackgroundService
{
    private readonly string _auditFilePath = "audit_log.jsonl";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 1. High-performance logging via source-generated extension method
        logger.LogWorkerStarting(_auditFilePath);

        // Continuously pull messages off the "Kafka Topic" as they arrive
        await foreach (var domainEvent in eventBus.SubscribeAsync(stoppingToken))
        {
            // 2. High-performance pattern-matched logging
            switch (domainEvent)
            {
                case TransactionCompleted txCompleted:
                    logger.LogTransactionCompleted(
                        txCompleted.Transaction.AccountId,
                        txCompleted.Transaction.Amount,
                        txCompleted.Transaction.Type,
                        txCompleted.EventId);
                    break;
                default:
                    logger.LogEventReceived(domainEvent.GetType().Name, domainEvent.EventId);
                    break;
            }

            // Serialize and persist
            var json = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
            await File.AppendAllTextAsync(_auditFilePath, json + Environment.NewLine, stoppingToken);

            // 3. High-performance logging
            logger.LogEventPersisted(_auditFilePath);
        }
    }
}
