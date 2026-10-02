using LedgerMock.Domain;
using LedgerMock.Infrastructure;
using LedgerMock.Workers;
using Amazon.DynamoDBv2;

var builder = WebApplication.CreateBuilder(args);

// 1. Setup AWS DynamoDB connection (Local for development, AWS Credential Chain for Production)
if (builder.Environment.IsDevelopment())
{
    var dynamoDbConfig = new AmazonDynamoDBConfig { ServiceURL = "http://localhost:8000" };
    builder.Services.AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient("dummy", "dummy", dynamoDbConfig));
}
else
{
    // Production/Staging: Picks up AWS credentials & region automatically (IAM Role, IRSA, ECS task role)
    builder.Services.AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient());
}

builder.Services.AddSingleton<DynamoDbStore>();
builder.Services.AddSingleton<ILedgerStore>(sp => sp.GetRequiredService<DynamoDbStore>());

// 2. Setup our Mock Kafka (EventBus) and Worker
builder.Services.AddSingleton<EventBus>();
builder.Services.AddHostedService<NotificationWorker>();

var app = builder.Build();

// 3. Ensure DynamoDB table is created automatically only during local development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var store = scope.ServiceProvider.GetRequiredService<DynamoDbStore>();
    await store.EnsureTableExistsAsync();
}

// 4. Map Endpoints
LedgerMock.Endpoints.MapLedgerEndpoints(app);

app.Run();
