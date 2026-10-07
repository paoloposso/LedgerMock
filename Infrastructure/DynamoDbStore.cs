using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using LedgerMock.Domain;

namespace LedgerMock.Infrastructure;

public class DynamoDbStore(IAmazonDynamoDB dynamoDb) : ILedgerStore
{
    public const string TableName = "LedgerEvents";

    // --- SETUP: Automatically creates the table for our local testing ---
    public async Task EnsureTableExistsAsync()
    {
        var tables = await dynamoDb.ListTablesAsync();
        if (tables.TableNames.Contains(TableName)) return;

        var request = new CreateTableRequest
        {
            TableName = TableName,
            AttributeDefinitions =
            [
                new AttributeDefinition("PK", ScalarAttributeType.S),
                new AttributeDefinition("SK", ScalarAttributeType.S),
                new AttributeDefinition("GSI1PK", ScalarAttributeType.S)
            ],
            KeySchema = 
            [
                new KeySchemaElement("PK", KeyType.HASH),
                new KeySchemaElement("SK", KeyType.RANGE)
            ],
            GlobalSecondaryIndexes =
            [
                new GlobalSecondaryIndex
                {
                    IndexName = "GSI1",
                    KeySchema =
                    [
                        new KeySchemaElement("GSI1PK", KeyType.HASH),
                        new KeySchemaElement("SK", KeyType.RANGE)
                    ],
                    Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    ProvisionedThroughput = new ProvisionedThroughput(1, 1)
                }
            ],
            ProvisionedThroughput = new ProvisionedThroughput(1, 1)
        };

        await dynamoDb.CreateTableAsync(request);
    }

    // --- 1. APPEND ONLY (Event Sourcing with Idempotency) ---
    public async Task<AppendResult> AppendTransactionAsync(Transaction tx, string idempotencyKey)
    {
        var request = new TransactWriteItemsRequest
        {
            TransactItems = [
                // Action 1: Write the actual ledger event
                new TransactWriteItem
                {
                    Put = new()
                    {
                        TableName = TableName,
                        Item = new Dictionary<string, AttributeValue>
                        {
                            { "PK", new AttributeValue { S = $"ACCOUNT#{tx.AccountId}" } },
                            { "SK", new AttributeValue { S = $"EVENT#{tx.Timestamp:O}#{tx.EventId}" } },
                            { "GSI1PK", new AttributeValue { S = $"TYPE#{tx.Type}" } },
                            { "AccountId", new AttributeValue { S = tx.AccountId } },
                            { "Amount", new AttributeValue { N = tx.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
                            { "Type", new AttributeValue { S = tx.Type.ToString() } },
                            { "Timestamp", new AttributeValue { S = tx.Timestamp.ToString("O") } },
                            { "EventId", new AttributeValue { S = tx.EventId } }
                        }
                    }
                },
                // Action 2: Write the Idempotency Lock
                new TransactWriteItem
                {
                    Put = new Put
                    {
                        TableName = TableName,
                        Item = new Dictionary<string, AttributeValue>
                        {
                            { "PK", new AttributeValue { S = $"IDEMPOTENCY#{idempotencyKey}" } },
                            { "SK", new AttributeValue { S = "LOCK" } },
                            { "CreatedAt", new AttributeValue { S = DateTimeOffset.UtcNow.ToString("O") } }
                        },
                        ConditionExpression = "attribute_not_exists(PK)"
                    }
                }
            ]
        };

        try
        {
            await dynamoDb.TransactWriteItemsAsync(request);
            return AppendResult.Success;
        }
        catch (TransactionCanceledException ex) when (ex.CancellationReasons.Any(r => r.Code == "ConditionalCheckFailed"))
        {
            return AppendResult.Duplicate;
        }
    }

    // --- 2. ATOMIC DOUBLE-ENTRY TRANSFER (Debit + Credit + Idempotency Lock) ---
    public async Task<AppendResult> AppendTransferAsync(Transaction debitTx, Transaction creditTx, string idempotencyKey)
    {
        var request = new TransactWriteItemsRequest
        {
            TransactItems = [
                // Leg 1: Debit source account (TransferOut)
                new()
                {
                    Put = new()
                    {
                        TableName = TableName,
                        Item = new Dictionary<string, AttributeValue>
                        {
                            { "PK", new AttributeValue { S = $"ACCOUNT#{debitTx.AccountId}" } },
                            { "SK", new AttributeValue { S = $"EVENT#{debitTx.Timestamp:O}#{debitTx.EventId}" } },
                            { "GSI1PK", new AttributeValue { S = $"TYPE#{debitTx.Type}" } },
                            { "AccountId", new AttributeValue { S = debitTx.AccountId } },
                            { "Amount", new AttributeValue { N = debitTx.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
                            { "Type", new AttributeValue { S = debitTx.Type.ToString() } },
                            { "Timestamp", new AttributeValue { S = debitTx.Timestamp.ToString("O") } },
                            { "EventId", new AttributeValue { S = debitTx.EventId } }
                        }
                    }
                },
                // Leg 2: Credit destination account (TransferIn)
                new()
                {
                    Put = new()
                    {
                        TableName = TableName,
                        Item = new Dictionary<string, AttributeValue>
                        {
                            { "PK", new AttributeValue { S = $"ACCOUNT#{creditTx.AccountId}" } },
                            { "SK", new AttributeValue { S = $"EVENT#{creditTx.Timestamp:O}#{creditTx.EventId}" } },
                            { "GSI1PK", new AttributeValue { S = $"TYPE#{creditTx.Type}" } },
                            { "AccountId", new AttributeValue { S = creditTx.AccountId } },
                            { "Amount", new AttributeValue { N = creditTx.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture) } },
                            { "Type", new AttributeValue { S = creditTx.Type.ToString() } },
                            { "Timestamp", new AttributeValue { S = creditTx.Timestamp.ToString("O") } },
                            { "EventId", new AttributeValue { S = creditTx.EventId } }
                        }
                    }
                },
                // Leg 3: Idempotency Lock
                new()
                {
                    Put = new()
                    {
                        TableName = TableName,
                        Item = new()
                        {
                            { "PK", new AttributeValue { S = $"IDEMPOTENCY#{idempotencyKey}" } },
                            { "SK", new AttributeValue { S = "LOCK" } },
                            { "CreatedAt", new AttributeValue { S = DateTimeOffset.UtcNow.ToString("O") } }
                        },
                        ConditionExpression = "attribute_not_exists(PK)"
                    }
                }
            ]
        };

        try
        {
            await dynamoDb.TransactWriteItemsAsync(request);
            return AppendResult.Success;
        }
        catch (TransactionCanceledException ex) when (ex.CancellationReasons.Any(r => r.Code == "ConditionalCheckFailed"))
        {
            return AppendResult.Duplicate;
        }
    }

    // --- 2. FETCH HISTORY ---
    public async Task<List<Transaction>> GetEventsAsync(string accountId)
    {
        var request = new QueryRequest()
        {
            TableName = TableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :skPrefix)",
            ExpressionAttributeValues = new()
            {
                { ":pk", new AttributeValue { S = $"ACCOUNT#{accountId}" } },
                { ":skPrefix", new AttributeValue { S = "EVENT#" } }
            }
        };

        var response = await dynamoDb.QueryAsync(request);

        return [..response.Items.Select(item => new Transaction(
            AccountId: item["AccountId"].S,
            Amount: decimal.Parse(item["Amount"].N, System.Globalization.CultureInfo.InvariantCulture),
            Type: Enum.Parse<TransactionType>(item["Type"].S),
            Timestamp: DateTimeOffset.Parse(item["Timestamp"].S),
            EventId: item["EventId"].S
        ))];
    }
}
