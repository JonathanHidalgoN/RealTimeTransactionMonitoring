using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Api.Controllers.V1;
using FinancialMonitoring.Models;
using FinancialMonitoring.Models.Analytics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinancialMonitoring.Api.Tests.V1;

public class AnalyticsControllerV1Tests
{
    private readonly AnalyticsController _controller;
    private readonly Mock<IAnalyticsRepository> _mockRepository;
    private readonly Mock<ILogger<AnalyticsController>> _mockLogger;

    public AnalyticsControllerV1Tests()
    {
        _mockRepository = new Mock<IAnalyticsRepository>();
        _mockLogger = new Mock<ILogger<AnalyticsController>>();

        _controller = new AnalyticsController(_mockRepository.Object, _mockLogger.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "test-correlation-id-v1";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task GetTransactionAnalytics_ReturnsOkResult_WithTransactionAnalytics()
    {
        var expectedAnalytics = new TransactionAnalytics(
            totalTransactions: 1000,
            totalAnomalies: 25,
            totalVolume: 500000.00,
            averageAmount: 500.00,
            uniqueAccounts: 150,
            transactionsLast24Hours: 100,
            anomaliesLast24Hours: 5);

        _mockRepository
            .Setup(service => service.GetTransactionAnalyticsAsync())
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
    public async Task GetTransactionTimeSeries_WithDefaultParameters_ReturnsTimeSeries()
    {
        var expectedTimeSeries = new List<TimeSeriesDataPoint>
        {
            new TimeSeriesDataPoint(DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds(), 100),
            new TimeSeriesDataPoint(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 150)
        };

        _mockRepository
            .Setup(service => service.GetTransactionTimeSeriesAsync(It.IsAny<long>(), It.IsAny<long>(), 60))
            .ReturnsAsync(expectedTimeSeries);

        var result = await _controller.GetTransactionTimeSeries();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<List<TimeSeriesDataPoint>>>(okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(2, apiResponse.Data.Count);
        Assert.NotNull(apiResponse.CorrelationId);
    }

    [Fact]
    public async Task GetTransactionTimeSeries_WithCustomParameters_ReturnsTimeSeries()
    {
        var hours = 48;
        var intervalMinutes = 30;
        var expectedTimeSeries = new List<TimeSeriesDataPoint>
        {
            new TimeSeriesDataPoint(DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeMilliseconds(), 75),
            new TimeSeriesDataPoint(DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds(), 80),
            new TimeSeriesDataPoint(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 90)
        };

        _mockRepository
            .Setup(service => service.GetTransactionTimeSeriesAsync(It.IsAny<long>(), It.IsAny<long>(), intervalMinutes))
            .ReturnsAsync(expectedTimeSeries);

        var result = await _controller.GetTransactionTimeSeries(hours, intervalMinutes);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<List<TimeSeriesDataPoint>>>(okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(3, apiResponse.Data.Count);
        Assert.NotNull(apiResponse.CorrelationId);
    }

    [Fact]
    public async Task GetAnomalyTimeSeries_WithDefaultParameters_ReturnsAnomalyTimeSeries()
    {
        var expectedTimeSeries = new List<TimeSeriesDataPoint>
        {
            new TimeSeriesDataPoint(DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds(), 5),
            new TimeSeriesDataPoint(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 3)
        };

        _mockRepository
            .Setup(service => service.GetAnomalyTimeSeriesAsync(It.IsAny<long>(), It.IsAny<long>(), 60))
            .ReturnsAsync(expectedTimeSeries);

        var result = await _controller.GetAnomalyTimeSeries();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<List<TimeSeriesDataPoint>>>(okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(2, apiResponse.Data.Count);
        Assert.NotNull(apiResponse.CorrelationId);
    }

    [Fact]
    public async Task GetTopMerchants_WithDefaultCount_ReturnsTopMerchants()
    {
        var expectedMerchants = new List<MerchantAnalytics>
        {
            new MerchantAnalytics("Store A", MerchantCategory.Retail, 500, 25000.00, 50.00, 10),
            new MerchantAnalytics("Store B", MerchantCategory.Grocery, 400, 20000.00, 50.00, 8),
            new MerchantAnalytics("Store C", MerchantCategory.Restaurant, 300, 15000.00, 50.00, 6)
        };

        _mockRepository
            .Setup(service => service.GetTopMerchantsAnalyticsAsync(10))
            .ReturnsAsync(expectedMerchants);

        var result = await _controller.GetTopMerchants();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<List<MerchantAnalytics>>>(okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(3, apiResponse.Data.Count);
        Assert.Equal("Store A", apiResponse.Data.First().MerchantName);
        Assert.NotNull(apiResponse.CorrelationId);
    }

    [Fact]
    public async Task GetTopMerchants_WithCustomCount_ReturnsTopMerchants()
    {
        var count = 5;
        var expectedMerchants = new List<MerchantAnalytics>
        {
            new MerchantAnalytics("Top Store", MerchantCategory.Retail, 1000, 50000.00, 50.00, 20)
        };

        _mockRepository
            .Setup(service => service.GetTopMerchantsAnalyticsAsync(count))
            .ReturnsAsync(expectedMerchants);

        var result = await _controller.GetTopMerchants(count);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<List<MerchantAnalytics>>>(okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Single(apiResponse.Data);
        Assert.Equal("Top Store", apiResponse.Data.First().MerchantName);
        Assert.NotNull(apiResponse.CorrelationId);
    }

    [Fact]
    public async Task GetMerchantCategoryAnalytics_ReturnsOkResult_WithCategoryAnalytics()
    {
        var expectedCategories = new List<MerchantAnalytics>
        {
            new MerchantAnalytics("Various", MerchantCategory.Retail, 800, 40000.00, 50.00, 16),
            new MerchantAnalytics("Various", MerchantCategory.Grocery, 600, 30000.00, 50.00, 12),
            new MerchantAnalytics("Various", MerchantCategory.Restaurant, 400, 20000.00, 50.00, 8)
        };

        _mockRepository
            .Setup(service => service.GetMerchantCategoryAnalyticsAsync())
            .ReturnsAsync(expectedCategories);

        var result = await _controller.GetMerchantCategoryAnalytics();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<List<MerchantAnalytics>>>(okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(3, apiResponse.Data.Count);
        Assert.Contains(apiResponse.Data, m => m.Category == MerchantCategory.Retail);
        Assert.Contains(apiResponse.Data, m => m.Category == MerchantCategory.Grocery);
        Assert.Contains(apiResponse.Data, m => m.Category == MerchantCategory.Restaurant);
        Assert.NotNull(apiResponse.CorrelationId);
    }
}
