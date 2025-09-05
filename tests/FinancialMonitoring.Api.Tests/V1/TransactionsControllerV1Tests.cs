using FinancialMonitoring.Models;
using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Api.Controllers.V1;
using FinancialMonitoring.Api.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;

namespace FinancialMonitoring.Api.Tests.V1;

public class TransactionsControllerV1Tests
{
    private readonly TransactionsController _controller;
    private readonly Mock<ITransactionRepository> _mockRepository;
    private readonly Mock<ILogger<TransactionsController>> _mockLogger;

    public TransactionsControllerV1Tests()
    {
        _mockRepository = new Mock<ITransactionRepository>();
        _mockLogger = new Mock<ILogger<TransactionsController>>();

        _controller = new TransactionsController(_mockRepository.Object, _mockLogger.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "test-correlation-id";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetAllTransactions_ReturnsOkResult_WithPagedResultOfTransactions()
    {
        var expectedTransactions = new List<Transaction>
        {
            new Transaction("tx1", 100, 0, new Account("ACC1"), new Account("ACC2"),
                TransactionType.Purchase, MerchantCategory.Retail, "Store 1", new Location("NYC", "NY", "US")),
            new Transaction("tx2", 200, 0, new Account("ACC3"), new Account("ACC4"),
                TransactionType.Purchase, MerchantCategory.Grocery, "Store 2", new Location("LA", "CA", "US"))
        };

        var expectedPagedResult = new PagedResult<Transaction>
        {
            Items = expectedTransactions,
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 2
        };

        var request = new TransactionQueryRequest { PageNumber = 1, PageSize = 2 };

        _mockRepository
            .Setup(service => service.GetAllTransactionsAsync(1, 2))
            .ReturnsAsync(expectedPagedResult);

        var result = await _controller.GetAllTransactions(request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<PagedResult<Transaction>>>(okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.NotNull(apiResponse.Data.Items);
        Assert.Equal(expectedPagedResult.TotalCount, apiResponse.Data.TotalCount);
        Assert.Equal(expectedTransactions.Count, apiResponse.Data.Items.Count);
        Assert.Contains(apiResponse.Data.Items, t => t.Id == "tx1");
        Assert.NotNull(apiResponse.CorrelationId);
        Assert.Equal(AppConstants.ApiVersion, apiResponse.Version);
    }

    [Fact]
    public async Task GetTransactionById_WhenTransactionExists_ReturnsOkResult_WithTransaction()
    {
        var transactionId = "existing-tx-id";
        var expectedTransaction = new Transaction(transactionId, 150, 0, new Account("ACC5"), new Account("ACC6"),
            TransactionType.Purchase, MerchantCategory.Restaurant, "Test Restaurant", new Location("NYC", "NY", "US"));

        _mockRepository
            .Setup(service => service.GetTransactionByIdAsync(transactionId))
            .ReturnsAsync(expectedTransaction);

        var result = await _controller.GetTransactionById(transactionId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<Transaction>>(okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(expectedTransaction.Id, apiResponse.Data.Id);
        Assert.NotNull(apiResponse.CorrelationId);
    }

    [Fact]
    public async Task GetTransactionById_WhenTransactionDoesNotExist_ReturnsNotFound()
    {
        var transactionId = "non-existing-tx-id";
        _mockRepository
            .Setup(service => service.GetTransactionByIdAsync(transactionId))
            .ReturnsAsync((Transaction?)null);

        var result = await _controller.GetTransactionById(transactionId);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ApiErrorResponse>(notFoundResult.Value);

        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);
        Assert.Equal(404, errorResponse.Error.Status);
        Assert.NotNull(errorResponse.CorrelationId);
    }

    [Theory]
    [InlineData("invalid-guid-format")]
    [InlineData("123-456")]
    public async Task GetTransactionById_WithValidIdFormat_WhenNotFound_ReturnsNotFound(string validIdFormat)
    {
        _mockRepository
            .Setup(service => service.GetTransactionByIdAsync(validIdFormat))
            .ReturnsAsync((Transaction?)null);

        var result = await _controller.GetTransactionById(validIdFormat);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ApiErrorResponse>(notFoundResult.Value);

        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);
        Assert.Equal(404, errorResponse.Error.Status);
        Assert.NotNull(errorResponse.CorrelationId);
    }

    [Fact]
    public async Task GetTransactionById_WithSpecialCharacters_HandlesCorrectly()
    {
        var testIds = new[] { "test-id-123", "abc_def", "TEST123" };

        foreach (var testId in testIds)
        {
            _mockRepository
                .Setup(service => service.GetTransactionByIdAsync(testId))
                .ReturnsAsync((Transaction?)null);

            var result = await _controller.GetTransactionById(testId);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var errorResponse = Assert.IsType<ApiErrorResponse>(notFoundResult.Value);

            Assert.False(errorResponse.Success);
            Assert.NotNull(errorResponse.Error);
            Assert.Equal(404, errorResponse.Error.Status);
            Assert.NotNull(errorResponse.CorrelationId);
        }
    }
}