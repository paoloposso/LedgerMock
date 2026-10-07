using System.Net;
using System.Net.Http.Json;
using LedgerMock.Domain;
using LedgerMock.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace LedgerMock.Tests.Endpoints;

public class EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ILedgerStore _mockStore = Substitute.For<ILedgerStore>();

    public EndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Replace ILedgerStore with our substitute for isolated unit tests
                var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ILedgerStore));
                if (descriptor is not null)
                {
                    services.Remove(descriptor);
                }
                services.AddSingleton(_mockStore);
            });
        });
    }

    [Fact]
    public async Task Deposit_WithValidPayload_ReturnsOkAndCallsStore()
    {
        // Arrange
        _mockStore.AppendTransactionAsync(Arg.Any<Transaction>(), Arg.Any<string>())
            .Returns(Task.FromResult(AppendResult.Success));

        var client = _factory.CreateClient();
        var request = new DepositRequest(100.00m, "dep-test-1");

        // Act
        var response = await client.PostAsJsonAsync("/accounts/acc-001/deposit", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await _mockStore.Received(1).AppendTransactionAsync(
            Arg.Is<Transaction>(tx => tx.AccountId == "acc-001" && tx.Amount == 100.00m && tx.Type == TransactionType.Deposit),
            "dep-test-1"
        );
    }

    [Fact]
    public async Task Deposit_MissingIdempotencyKey_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new DepositRequest(100.00m, "");

        // Act
        var response = await client.PostAsJsonAsync("/accounts/acc-001/deposit", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Deposit_WithNegativeAmount_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new DepositRequest(-10.00m, "dep-invalid");

        // Act
        var response = await client.PostAsJsonAsync("/accounts/acc-001/deposit", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Withdraw_WithValidPayload_ReturnsOkAndCallsStore()
    {
        // Arrange
        _mockStore.AppendTransactionAsync(Arg.Any<Transaction>(), Arg.Any<string>())
            .Returns(Task.FromResult(AppendResult.Success));

        var client = _factory.CreateClient();
        var request = new WithdrawRequest(45.50m, "wd-test-1");

        // Act
        var response = await client.PostAsJsonAsync("/accounts/acc-001/withdraw", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await _mockStore.Received(1).AppendTransactionAsync(
            Arg.Is<Transaction>(tx => tx.AccountId == "acc-001" && tx.Amount == 45.50m && tx.Type == TransactionType.Withdrawal),
            "wd-test-1"
        );
    }

    [Fact]
    public async Task Transfer_WithValidPayload_ExecutesAtomicDebitAndCreditLegs()
    {
        // Arrange
        _mockStore.AppendTransferAsync(Arg.Any<Transaction>(), Arg.Any<Transaction>(), Arg.Any<string>())
            .Returns(Task.FromResult(AppendResult.Success));

        var client = _factory.CreateClient();
        var request = new TransferRequest("acc-001", "acc-002", 75.00m, "txf-test-1");

        // Act
        var response = await client.PostAsJsonAsync("/transfers", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await _mockStore.Received(1).AppendTransferAsync(
            Arg.Is<Transaction>(tx => tx.AccountId == "acc-001" && tx.Amount == 75.00m && tx.Type == TransactionType.TransferOut),
            Arg.Is<Transaction>(tx => tx.AccountId == "acc-002" && tx.Amount == 75.00m && tx.Type == TransactionType.TransferIn),
            "txf-test-1"
        );
    }

    [Fact]
    public async Task Transfer_WhenDuplicateIdempotencyKey_ReturnsOkWithoutThrowing()
    {
        // Arrange
        _mockStore.AppendTransferAsync(Arg.Any<Transaction>(), Arg.Any<Transaction>(), Arg.Any<string>())
            .Returns(Task.FromResult(AppendResult.Duplicate));

        var client = _factory.CreateClient();
        var request = new TransferRequest("acc-001", "acc-002", 75.00m, "txf-dup-key");

        // Act
        var response = await client.PostAsJsonAsync("/transfers", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Transfer_WithSameSourceAndDestination_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new TransferRequest("acc-001", "acc-001", 50.00m, "txf-same-acc");

        // Act
        var response = await client.PostAsJsonAsync("/transfers", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetBalance_CalculatesBalanceFromHistory()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        List<Transaction> history =
        [
            new("acc-001", 100.00m, TransactionType.Deposit, now.AddMinutes(-10), "evt-1"),
            new("acc-001", 30.00m, TransactionType.Withdrawal, now.AddMinutes(-5), "evt-2")
        ];

        _mockStore.GetEventsAsync("acc-001").Returns(Task.FromResult(history));

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/accounts/acc-001/balance");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var state = await response.Content.ReadFromJsonAsync<AccountState>();
        Assert.NotNull(state);
        Assert.Equal("acc-001", state.AccountId);
        Assert.Equal(70.00m, state.Balance);
    }
}
