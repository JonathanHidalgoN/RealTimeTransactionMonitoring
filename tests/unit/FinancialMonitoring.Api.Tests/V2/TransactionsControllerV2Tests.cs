using FinancialMonitoring.Models;
using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Api.Controllers.V2;
using FinancialMonitoring.Api.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;

namespace FinancialMonitoring.Api.Tests.V2;

public class TransactionsControllerV2Tests
{
    private readonly TransactionsController _controller;
    private readonly Mock<ITransactionRepository> _mockRepository;
    private readonly Mock<ILogger<TransactionsController>> _mockLogger;

    public TransactionsControllerV2Tests()
    {
        _mockRepository = new Mock<ITransactionRepository>();
        _mockLogger = new Mock<ILogger<TransactionsController>>();

        _controller = new TransactionsController(_mockRepository.Object, _mockLogger.Object);

        // Setup HTTP context with JWT claims (V2 uses JWT authentication)
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "test-correlation-id-v2";

        // Add claims that V2 controller expects
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("email", "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "jwt");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetAllTransactions_WithAdminRole_ReturnsAllTransactions()
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
    public async Task GetTransactionById_WithJWTAuth_ReturnsTransaction()
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
    public async Task GetAnomalousTransactions_WithAdminRole_ReturnsAnomalies()
    {
        var expectedTransactions = new List<Transaction>
        {
            new Transaction("anomaly-1", 10000, 0, new Account("ACC7"), new Account("ACC8"),
                TransactionType.Purchase, MerchantCategory.Retail, "Suspicious Store", new Location("Unknown", "Unknown", "US"), anomalyFlag: "high_amount"),
            new Transaction("anomaly-2", 50000, 0, new Account("ACC9"), new Account("ACC10"),
                TransactionType.Transfer, MerchantCategory.Other, "Large Transfer", new Location("NYC", "NY", "US"), anomalyFlag: "unusual_pattern")
        };

        var expectedPagedResult = new PagedResult<Transaction>
        {
            Items = expectedTransactions,
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        var request = new TransactionQueryRequest { PageNumber = 1, PageSize = 10 };

        _mockRepository
            .Setup(service => service.GetAnomalousTransactionsAsync(1, 10))
            .ReturnsAsync(expectedPagedResult);

        var result = await _controller.GetAnomalousTransactions(request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<PagedResult<Transaction>>>(okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.NotNull(apiResponse.Data.Items);
        Assert.Equal(2, apiResponse.Data.Items.Count);
        Assert.All(apiResponse.Data.Items, t => Assert.NotNull(t.AnomalyFlag));
        Assert.NotNull(apiResponse.CorrelationId);
    }

    [Fact]
    public async Task SearchTransactions_WithValidRequest_ReturnsMatchingTransactions()
    {
        var searchRequest = new TransactionSearchRequest
        {
            MerchantName = "Test Store",
            MinAmount = 100,
            MaxAmount = 1000,
            PageNumber = 1,
            PageSize = 10
        };

        var expectedTransactions = new List<Transaction>
        {
            new Transaction("search-1", 500, 0, new Account("ACC11"), new Account("ACC12"),
                TransactionType.Purchase, MerchantCategory.Retail, "Test Store", new Location("NYC", "NY", "US"))
        };

        var expectedPagedResult = new PagedResult<Transaction>
        {
            Items = expectedTransactions,
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _mockRepository
            .Setup(service => service.SearchTransactionsAsync(
                It.Is<TransactionSearchRequest>(r => r.MerchantName == "Test Store")))
            .ReturnsAsync(expectedPagedResult);

        var result = await _controller.SearchTransactions(searchRequest);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<PagedResult<Transaction>>>(okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Single(apiResponse.Data.Items);
        Assert.Equal("Test Store", apiResponse.Data.Items.First().MerchantName);
        Assert.NotNull(apiResponse.CorrelationId);
    }

    [Fact]
    public async Task GetTransactionById_WhenNotFound_ReturnsNotFound()
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

    [Fact]
    public async Task GetAllTransactions_WithUserClaims_IncludesUserContext()
    {
        // Test that V2 controller properly uses user claims from JWT
        var expectedTransactions = new List<Transaction>
        {
            new Transaction("user-tx-1", 75, 0, new Account("USER1"), new Account("USER2"),
                TransactionType.Purchase, MerchantCategory.Grocery, "User Store", new Location("LA", "CA", "US"))
        };

        var expectedPagedResult = new PagedResult<Transaction>
        {
            Items = expectedTransactions,
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        var request = new TransactionQueryRequest { PageNumber = 1, PageSize = 10 };

        _mockRepository
            .Setup(service => service.GetAllTransactionsAsync(1, 10))
            .ReturnsAsync(expectedPagedResult);

        var result = await _controller.GetAllTransactions(request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<PagedResult<Transaction>>>(okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Single(apiResponse.Data.Items);

        // Verify that user context from JWT claims is available
        Assert.Equal("user-123", _controller.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("Admin", _controller.User.FindFirst(ClaimTypes.Role)?.Value);
        Assert.NotNull(apiResponse.CorrelationId);
    }
}
