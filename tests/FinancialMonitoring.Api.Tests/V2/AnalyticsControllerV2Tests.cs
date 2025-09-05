using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Api.Controllers.V2;
using FinancialMonitoring.Models;
using FinancialMonitoring.Models.Analytics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace FinancialMonitoring.Api.Tests.V2;

public class AnalyticsControllerV2Tests
{
    private readonly AnalyticsController _controller;
    private readonly Mock<IAnalyticsRepository> _mockRepository;
    private readonly Mock<ILogger<AnalyticsController>> _mockLogger;

    public AnalyticsControllerV2Tests()
    {
        _mockRepository = new Mock<IAnalyticsRepository>();
        _mockLogger = new Mock<ILogger<AnalyticsController>>();
        
        _controller = new AnalyticsController(_mockRepository.Object, _mockLogger.Object);
        
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "test-correlation-id";
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "123"),
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        httpContext.User = claimsPrincipal;
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetTransactionAnalytics_ReturnsOkResult_WithAnalytics()
    {
        var expectedAnalytics = new TransactionAnalytics(
            totalTransactions: 1000,
            totalAnomalies: 25,
            totalVolume: 50000.00,
            averageAmount: 50.00,
            uniqueAccounts: 500,
            transactionsLast24Hours: 100,
            anomaliesLast24Hours: 5);

        _mockRepository
            .Setup(repo => repo.GetTransactionAnalyticsAsync())
            .ReturnsAsync(expectedAnalytics);

        var result = await _controller.GetTransactionAnalytics();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<TransactionAnalytics>>(okResult.Value);
        
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(expectedAnalytics.TotalTransactions, apiResponse.Data.TotalTransactions);
        Assert.Equal(expectedAnalytics.TotalVolume, apiResponse.Data.TotalVolume);
        Assert.Equal(expectedAnalytics.TotalAnomalies, apiResponse.Data.TotalAnomalies);
        Assert.NotNull(apiResponse.CorrelationId);
    }

    [Fact]
    public async Task GetTransactionAnalytics_WhenRepositoryReturnsNull_ReturnsInternalServerError()
    {
        _mockRepository
            .Setup(repo => repo.GetTransactionAnalyticsAsync())
            .Returns(Task.FromResult<TransactionAnalytics>(null!));

        var result = await _controller.GetTransactionAnalytics();

        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        
        var errorResponse = Assert.IsType<ApiErrorResponse>(statusCodeResult.Value);
        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);
        Assert.Equal(500, errorResponse.Error.Status);
        Assert.NotNull(errorResponse.CorrelationId);
    }

    [Fact]
    public async Task GetTransactionTimeSeries_WithDefaultParameters_ReturnsOkResult()
    {
        var expectedTimeSeries = new List<TimeSeriesDataPoint>
        {
            new TimeSeriesDataPoint(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 100),
            new TimeSeriesDataPoint(DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds(), 85)
        };

        _mockRepository
            .Setup(repo => repo.GetTransactionTimeSeriesAsync(It.IsAny<long>(), It.IsAny<long>(), 60))
            .ReturnsAsync(expectedTimeSeries);

        var result = await _controller.GetTransactionTimeSeries();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<List<TimeSeriesDataPoint>>>(okResult.Value);
        
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(expectedTimeSeries.Count, apiResponse.Data.Count);
        Assert.NotNull(apiResponse.CorrelationId);
    }

    [Fact]
    public async Task GetTransactionTimeSeries_WithCustomParameters_ReturnsOkResult()
    {
        var hours = 48;
        var intervalMinutes = 120;
        var expectedTimeSeries = new List<TimeSeriesDataPoint>
        {
            new TimeSeriesDataPoint(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 200)
        };

        _mockRepository
            .Setup(repo => repo.GetTransactionTimeSeriesAsync(It.IsAny<long>(), It.IsAny<long>(), intervalMinutes))
            .ReturnsAsync(expectedTimeSeries);

        var result = await _controller.GetTransactionTimeSeries(hours, intervalMinutes);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<List<TimeSeriesDataPoint>>>(okResult.Value);
        
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(expectedTimeSeries.Count, apiResponse.Data.Count);
        Assert.NotNull(apiResponse.CorrelationId);
    }

    [Fact]
    public async Task GetTransactionTimeSeries_WhenRepositoryReturnsNull_ReturnsInternalServerError()
    {
        _mockRepository
            .Setup(repo => repo.GetTransactionTimeSeriesAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>()))
            .Returns(Task.FromResult<List<TimeSeriesDataPoint>>(null!));

        var result = await _controller.GetTransactionTimeSeries();

        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        
        var errorResponse = Assert.IsType<ApiErrorResponse>(statusCodeResult.Value);
        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);
        Assert.Equal(500, errorResponse.Error.Status);
        Assert.NotNull(errorResponse.CorrelationId);
    }

    [Fact]
    public async Task GetAnomalyTimeSeries_WithDefaultParameters_ReturnsOkResult()
    {
        var expectedTimeSeries = new List<TimeSeriesDataPoint>
        {
            new TimeSeriesDataPoint(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 5),
            new TimeSeriesDataPoint(DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds(), 3)
        };

        _mockRepository
            .Setup(repo => repo.GetAnomalyTimeSeriesAsync(It.IsAny<long>(), It.IsAny<long>(), 60))
            .ReturnsAsync(expectedTimeSeries);

        var result = await _controller.GetAnomalyTimeSeries();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<List<TimeSeriesDataPoint>>>(okResult.Value);
        
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(expectedTimeSeries.Count, apiResponse.Data.Count);
        Assert.NotNull(apiResponse.CorrelationId);
    }

    [Fact]
    public async Task GetAnomalyTimeSeries_WithCustomParameters_ReturnsOkResult()
    {
        var hours = 72;
        var intervalMinutes = 30;
        var expectedTimeSeries = new List<TimeSeriesDataPoint>
        {
            new TimeSeriesDataPoint(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 8)
        };

        _mockRepository
            .Setup(repo => repo.GetAnomalyTimeSeriesAsync(It.IsAny<long>(), It.IsAny<long>(), intervalMinutes))
            .ReturnsAsync(expectedTimeSeries);

        var result = await _controller.GetAnomalyTimeSeries(hours, intervalMinutes);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<List<TimeSeriesDataPoint>>>(okResult.Value);
        
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(expectedTimeSeries.Count, apiResponse.Data.Count);
        Assert.NotNull(apiResponse.CorrelationId);
    }

    [Fact]
    public async Task GetAnomalyTimeSeries_WhenRepositoryReturnsNull_ReturnsInternalServerError()
    {
        _mockRepository
            .Setup(repo => repo.GetAnomalyTimeSeriesAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>()))
            .Returns(Task.FromResult<List<TimeSeriesDataPoint>>(null!));

        var result = await _controller.GetAnomalyTimeSeries();

        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        
        var errorResponse = Assert.IsType<ApiErrorResponse>(statusCodeResult.Value);
        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);
        Assert.Equal(500, errorResponse.Error.Status);
        Assert.NotNull(errorResponse.CorrelationId);
    }

    [Fact]
    public async Task GetTopMerchants_WithDefaultParameters_ReturnsOkResult()
    {
        var expectedMerchants = new List<MerchantAnalytics>
        {
            new MerchantAnalytics("Store A", MerchantCategory.Retail, 500, 25000.00, 50.00, 5),
            new MerchantAnalytics("Store B", MerchantCategory.Grocery, 400, 20000.00, 50.00, 4)
        };

        _mockRepository
            .Setup(repo => repo.GetTopMerchantsAnalyticsAsync(10))
            .ReturnsAsync(expectedMerchants);

        var result = await _controller.GetTopMerchants();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<List<MerchantAnalytics>>>(okResult.Value);
        
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(expectedMerchants.Count, apiResponse.Data.Count);
        Assert.Equal("Store A", apiResponse.Data[0].MerchantName);
        Assert.NotNull(apiResponse.CorrelationId);
    }

    [Fact]
    public async Task GetTopMerchants_WithCustomCount_ReturnsOkResult()
    {
        var count = 25;
        var expectedMerchants = new List<MerchantAnalytics>
        {
            new MerchantAnalytics("Top Store", MerchantCategory.Retail, 1000, 50000.00, 50.00, 10)
        };

        _mockRepository
            .Setup(repo => repo.GetTopMerchantsAnalyticsAsync(count))
            .ReturnsAsync(expectedMerchants);

        var result = await _controller.GetTopMerchants(count);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<List<MerchantAnalytics>>>(okResult.Value);
        
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(expectedMerchants.Count, apiResponse.Data.Count);
        Assert.NotNull(apiResponse.CorrelationId);
    }

    [Fact]
    public async Task GetTopMerchants_WhenRepositoryReturnsNull_ReturnsInternalServerError()
    {
        _mockRepository
            .Setup(repo => repo.GetTopMerchantsAnalyticsAsync(It.IsAny<int>()))
            .Returns(Task.FromResult<List<MerchantAnalytics>>(null!));

        var result = await _controller.GetTopMerchants();

        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        
        var errorResponse = Assert.IsType<ApiErrorResponse>(statusCodeResult.Value);
        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);
        Assert.Equal(500, errorResponse.Error.Status);
        Assert.NotNull(errorResponse.CorrelationId);
    }

    [Fact]
    public async Task GetMerchantCategoryAnalytics_ReturnsOkResult_WithCategoryAnalytics()
    {
        var expectedCategoryAnalytics = new List<MerchantAnalytics>
        {
            new MerchantAnalytics("Various", MerchantCategory.Retail, 500, 25000.00, 50.00, 5),
            new MerchantAnalytics("Various", MerchantCategory.Grocery, 300, 15000.00, 50.00, 3),
            new MerchantAnalytics("Various", MerchantCategory.Restaurant, 200, 10000.00, 50.00, 2)
        };

        _mockRepository
            .Setup(repo => repo.GetMerchantCategoryAnalyticsAsync())
            .ReturnsAsync(expectedCategoryAnalytics);

        var result = await _controller.GetMerchantCategoryAnalytics();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<List<MerchantAnalytics>>>(okResult.Value);
        
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(expectedCategoryAnalytics.Count, apiResponse.Data.Count);
        Assert.Equal(MerchantCategory.Retail, apiResponse.Data[0].Category);
        Assert.NotNull(apiResponse.CorrelationId);
    }

    [Fact]
    public async Task GetMerchantCategoryAnalytics_WhenRepositoryReturnsNull_ReturnsInternalServerError()
    {
        _mockRepository
            .Setup(repo => repo.GetMerchantCategoryAnalyticsAsync())
            .Returns(Task.FromResult<List<MerchantAnalytics>>(null!));

        var result = await _controller.GetMerchantCategoryAnalytics();

        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        
        var errorResponse = Assert.IsType<ApiErrorResponse>(statusCodeResult.Value);
        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);
        Assert.Equal(500, errorResponse.Error.Status);
        Assert.NotNull(errorResponse.CorrelationId);
    }

    [Fact]
    public async Task GetTransactionAnalytics_LogsCorrectInformation()
    {
        var expectedAnalytics = new TransactionAnalytics(
            totalTransactions: 100,
            totalAnomalies: 5,
            totalVolume: 5000.00,
            averageAmount: 50.00,
            uniqueAccounts: 50,
            transactionsLast24Hours: 20,
            anomaliesLast24Hours: 1);

        _mockRepository
            .Setup(repo => repo.GetTransactionAnalyticsAsync())
            .ReturnsAsync(expectedAnalytics);

        await _controller.GetTransactionAnalytics();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Secure analytics overview query by user 123 with role Admin")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTransactionTimeSeries_LogsCorrectInformation()
    {
        var expectedTimeSeries = new List<TimeSeriesDataPoint>
        {
            new TimeSeriesDataPoint(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 100)
        };

        _mockRepository
            .Setup(repo => repo.GetTransactionTimeSeriesAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>()))
            .ReturnsAsync(expectedTimeSeries);

        await _controller.GetTransactionTimeSeries(48, 120);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Secure transaction time series query by user 123 with role Admin for 48 hours with 120 minute intervals")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}