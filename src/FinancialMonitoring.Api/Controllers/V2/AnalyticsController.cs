using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Api.Authentication;
using FinancialMonitoring.Models;
using FinancialMonitoring.Models.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FinancialMonitoring.Api.Controllers.V2;

/// <summary>
/// API endpoints for transaction analytics and statistics (V1 - API Key authentication).
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/analytics")]
[Authorize(AuthenticationSchemes = SecureApiKeyAuthenticationDefaults.SchemeName)]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly ILogger<AnalyticsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyticsController"/> class.
    /// </summary>
    /// <param name="analyticsRepository">The repository for analytics operations.</param>
    /// <param name="logger">The logger for recording operational information.</param>
    public AnalyticsController(IAnalyticsRepository analyticsRepository, ILogger<AnalyticsController> logger)
    {
        _analyticsRepository = analyticsRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets global transaction analytics and statistics.
    /// </summary>
    /// <returns>Global transaction analytics.</returns>
    [HttpGet("overview")]
    [Microsoft.AspNetCore.OutputCaching.OutputCache(PolicyName = "AnalyticsCache")]
    [ProducesResponseType(typeof(ApiResponse<TransactionAnalytics>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TransactionAnalytics>>> GetTransactionAnalytics()
    {
        var correlationId = HttpContext.TraceIdentifier;
        _logger.LogInformation("Getting transaction analytics overview, CorrelationId: {CorrelationId}", correlationId);

        var analytics = await _analyticsRepository.GetTransactionAnalyticsAsync();
        var response = ApiResponse<TransactionAnalytics>.SuccessResponse(analytics, correlationId);

        return Ok(response);
    }

    /// <summary>
    /// Gets time series data for transaction counts over a specified period.
    /// </summary>
    /// <param name="hours">Number of hours to look back from now.</param>
    /// <param name="intervalMinutes">Interval in minutes for data points (default: 60).</param>
    /// <returns>Time series data for transaction counts.</returns>
    [HttpGet("timeseries/transactions")]
    [Microsoft.AspNetCore.OutputCaching.OutputCache(PolicyName = "TimeSeriesCache")]
    [ProducesResponseType(typeof(ApiResponse<List<TimeSeriesDataPoint>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<TimeSeriesDataPoint>>>> GetTransactionTimeSeries(
        [FromQuery, Range(1, 168)] int hours = 24,
        [FromQuery, Range(1, 1440)] int intervalMinutes = 60)
    {
        var correlationId = HttpContext.TraceIdentifier;
        _logger.LogInformation("Getting transaction time series for {Hours} hours with {IntervalMinutes} minute intervals, CorrelationId: {CorrelationId}",
            hours, intervalMinutes, correlationId);

        var toTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var fromTimestamp = DateTimeOffset.UtcNow.AddHours(-hours).ToUnixTimeMilliseconds();

        var timeSeries = await _analyticsRepository.GetTransactionTimeSeriesAsync(fromTimestamp, toTimestamp, intervalMinutes);
        var response = ApiResponse<List<TimeSeriesDataPoint>>.SuccessResponse(timeSeries, correlationId);

        return Ok(response);
    }

    /// <summary>
    /// Gets time series data for anomaly counts over a specified period.
    /// </summary>
    /// <param name="hours">Number of hours to look back from now.</param>
    /// <param name="intervalMinutes">Interval in minutes for data points (default: 60).</param>
    /// <returns>Time series data for anomaly counts.</returns>
    [HttpGet("timeseries/anomalies")]
    [Microsoft.AspNetCore.OutputCaching.OutputCache(PolicyName = "TimeSeriesCache")]
    [ProducesResponseType(typeof(ApiResponse<List<TimeSeriesDataPoint>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<TimeSeriesDataPoint>>>> GetAnomalyTimeSeries(
        [FromQuery, Range(1, 168)] int hours = 24,
        [FromQuery, Range(1, 1440)] int intervalMinutes = 60)
    {
        var correlationId = HttpContext.TraceIdentifier;
        _logger.LogInformation("Getting anomaly time series for {Hours} hours with {IntervalMinutes} minute intervals, CorrelationId: {CorrelationId}",
            hours, intervalMinutes, correlationId);

        var toTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var fromTimestamp = DateTimeOffset.UtcNow.AddHours(-hours).ToUnixTimeMilliseconds();

        var timeSeries = await _analyticsRepository.GetAnomalyTimeSeriesAsync(fromTimestamp, toTimestamp, intervalMinutes);
        var response = ApiResponse<List<TimeSeriesDataPoint>>.SuccessResponse(timeSeries, correlationId);

        return Ok(response);
    }

    /// <summary>
    /// Gets analytics data for top merchants by transaction volume.
    /// </summary>
    /// <param name="count">Number of top merchants to return (default: 10).</param>
    /// <returns>Analytics data for top merchants.</returns>
    [HttpGet("merchants/top")]
    [Microsoft.AspNetCore.OutputCaching.OutputCache(PolicyName = "AnalyticsCache")]
    [ProducesResponseType(typeof(ApiResponse<List<MerchantAnalytics>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<MerchantAnalytics>>>> GetTopMerchants(
        [FromQuery, Range(1, 50)] int count = 10)
    {
        var correlationId = HttpContext.TraceIdentifier;
        _logger.LogInformation("Getting top {Count} merchants analytics, CorrelationId: {CorrelationId}", count, correlationId);

        var merchantAnalytics = await _analyticsRepository.GetTopMerchantsAnalyticsAsync(count);
        var response = ApiResponse<List<MerchantAnalytics>>.SuccessResponse(merchantAnalytics, correlationId);

        return Ok(response);
    }

    /// <summary>
    /// Gets analytics data grouped by merchant category.
    /// </summary>
    /// <returns>Analytics data grouped by merchant category.</returns>
    [HttpGet("merchants/categories")]
    [Microsoft.AspNetCore.OutputCaching.OutputCache(PolicyName = "AnalyticsCache")]
    [ProducesResponseType(typeof(ApiResponse<List<MerchantAnalytics>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<MerchantAnalytics>>>> GetMerchantCategoryAnalytics()
    {
        var correlationId = HttpContext.TraceIdentifier;
        _logger.LogInformation("Getting merchant category analytics, CorrelationId: {CorrelationId}", correlationId);

        var categoryAnalytics = await _analyticsRepository.GetMerchantCategoryAnalyticsAsync();
        var response = ApiResponse<List<MerchantAnalytics>>.SuccessResponse(categoryAnalytics, correlationId);

        return Ok(response);
    }
}