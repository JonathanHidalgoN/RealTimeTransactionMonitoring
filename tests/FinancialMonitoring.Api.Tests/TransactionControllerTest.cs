using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using FinancialMonitoring.Models;
using FinancialMonitoring.Abstractions.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace FinancialMonitoring.Api.Tests;

public class TransactionsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Mock<ITransactionQueryService> _mockQueryService;

    public TransactionsControllerTests(WebApplicationFactory<Program> factory)
    {
        _mockQueryService = new Mock<ITransactionQueryService>();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ITransactionQueryService>();
                services.AddSingleton<ITransactionQueryService>(_mockQueryService.Object);
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllTransactions_ReturnsOkResult_WithListOfTransactions()
    {
        var expectedTransactions = new List<Transaction>
        {
            new Transaction("tx1", 100, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), new Account("ACC1"), new Account("ACC2")),
            new Transaction("tx2", 200, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), new Account("ACC3"), new Account("ACC4"))
        };

        _mockQueryService
            .Setup(service => service.GetAllTransactionsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(expectedTransactions);

        var response = await _client.GetAsync("/api/transactions?pageNumber=1&pageSize=2");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var actualTransactions = await response.Content.ReadFromJsonAsync<List<Transaction>>();
        Assert.NotNull(actualTransactions);
        Assert.Equal(expectedTransactions.Count, actualTransactions.Count);
        Assert.Contains(actualTransactions, t => t.Id == "tx1");
        Assert.Contains(actualTransactions, t => t.Id == "tx2");
    }

    [Fact]
    public async Task GetTransactionById_WhenTransactionExists_ReturnsOkResult_WithTransaction()
    {
        var transactionId = "existing-tx-id";
        var expectedTransaction = new Transaction(transactionId, 150, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), new Account("ACC5"), new Account("ACC6"));

        _mockQueryService
            .Setup(service => service.GetTransactionByIdAsync(transactionId))
            .ReturnsAsync(expectedTransaction);

        var response = await _client.GetAsync($"/api/transactions/{transactionId}");

        response.EnsureSuccessStatusCode();
        var actualTransaction = await response.Content.ReadFromJsonAsync<Transaction>();
        Assert.NotNull(actualTransaction);
        Assert.Equal(expectedTransaction.Id, actualTransaction.Id);
        Assert.Equal(expectedTransaction.Amount, actualTransaction.Amount);
    }

    [Fact]
    public async Task GetTransactionById_WhenTransactionDoesNotExist_ReturnsNotFound()
    {
        var transactionId = "non-existing-tx-id";
        _mockQueryService
            .Setup(service => service.GetTransactionByIdAsync(transactionId))
            .ReturnsAsync((Transaction?)null);

        var response = await _client.GetAsync($"/api/transactions/{transactionId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}