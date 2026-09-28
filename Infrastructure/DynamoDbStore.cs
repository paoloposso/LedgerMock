using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using LedgerMock.Domain;

namespace LedgerMock.Infrastructure;

// This is part of the "Imperative Shell". 
// It handles the dirty I/O work of talking to DynamoDB.
public class DynamoDbStore(IAmazonDynamoDB dynamoDb)
{
    private readonly IAmazonDynamoDB _dynamoDb = dynamoDb;
    public const string TableName = "LedgerEvents";

    // --- 1. APPEND ONLY (Event Sourcing) ---
    // We NEVER update an existing row. We only append new facts.
    public async Task AppendTransactionAsync(Transaction tx)
    {
        var request = new PutItemRequest
        {
            TableName = TableName,
            Item = new Dictionary<string, AttributeValue>
            {
                // Base Table Keys
                { "PK", new AttributeValue { S = $"ACCOUNT#{tx.AccountId}" } },
                { "SK", new AttributeValue { S = $"EVENT#{tx.Timestamp:O}#{tx.EventId}" } },
                
                // GSI1 (Global Secondary Index) Keys - For Analytical Queries
                { "GSI1PK", new AttributeValue { S = $"TYPE#{tx.Type}" } },
                
                // Event Payload
                { "AccountId", new AttributeValue { S = tx.AccountId } },
                { "Amount", new AttributeValue { N = tx.Amount.ToString() } },
                { "Type", new AttributeValue { S = tx.Type.ToString() } },
                { "Timestamp", new AttributeValue { S = tx.Timestamp.ToString("O") } },
                { "EventId", new AttributeValue { S = tx.EventId } }
            }
        };

        await _dynamoDb.PutItemAsync(request);
    }

    // --- 2. FETCH HISTORY ---
    // Fetches the pure data, which we will later hand to our pure Functional Core.
    public async Task<List<Transaction>> GetEventsAsync(string accountId)
    {
        var request = new QueryRequest
        {
            TableName = TableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :skPrefix)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":pk", new AttributeValue { S = $"ACCOUNT#{accountId}" } },
                { ":skPrefix", new AttributeValue { S = "EVENT#" } }
            }
        };

        var response = await _dynamoDb.QueryAsync(request);

        // Map DynamoDB dictionary back to our pure domain record
        return response.Items.Select(item => new Transaction(
            AccountId: item["AccountId"].S,
            Amount: decimal.Parse(item["Amount"].N),
            Type: Enum.Parse<TransactionType>(item["Type"].S),
            Timestamp: DateTimeOffset.Parse(item["Timestamp"].S),
            EventId: item["EventId"].S
        )).ToList();
    }
}
