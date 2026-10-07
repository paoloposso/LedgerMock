using LedgerMock.Domain;

namespace LedgerMock.Workers;

public static partial class WorkerLoggerExtensions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "--- NotificationWorker starting. Audit logs will be saved to {AuditFilePath} ---")]
    public static partial void LogWorkerStarting(this ILogger logger, string auditFilePath);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "[WORKER] Received Event: {EventType} for ID {EventId}")]
    public static partial void LogEventReceived(this ILogger logger, string eventType, string eventId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "[WORKER] Successfully persisted event to {AuditFilePath} for debugging.\n")]
    public static partial void LogEventPersisted(this ILogger logger, string auditFilePath);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "[NOTIFICATION SERVICE] Received Event: TransactionCompleted for Account {AccountId} | AmountInCents: {AmountInCents} | Type: {Type} | EventId: {EventId}")]
    public static partial void LogTransactionCompleted(this ILogger logger, string accountId, long amountInCents, TransactionType type, string eventId);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "[NOTIFICATION SERVICE] Received Event: TransferCompleted from {SourceAccountId} to {DestinationAccountId} | AmountInCents: {AmountInCents} | EventId: {EventId}")]
    public static partial void LogTransferCompleted(this ILogger logger, string sourceAccountId, string destinationAccountId, long amountInCents, string eventId);
}
