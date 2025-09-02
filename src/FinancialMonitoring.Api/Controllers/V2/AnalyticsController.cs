using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Models;
using FinancialMonitoring.Models.Analytics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace FinancialMonitoring.Api.Controllers.V2;

/// <summary>
/// API endpoints for transaction analytics and statistics (V2 - JWT authentication with role-based authorization).
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/analytics")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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
    /// Gets global transaction analytics and statistics (Admin and Analyst only).
    /// </summary>
    /// <returns>Global transaction analytics.</returns>
    [HttpGet("overview")]
    [Authorize(Roles = AppConstants.AdminAnalystRoles)]
    [Microsoft.AspNetCore.OutputCaching.OutputCache(PolicyName = AppConstants.AnalyticsCachePolicy)]
    [ProducesResponseType(typeof(ApiResponse<TransactionAnalytics>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TransactionAnalytics>>> GetTransactionAnalytics()
    {
        var correlationId = HttpContext.TraceIdentifier;
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        _logger.LogInformation("Secure analytics overview query by user {UserId} with role {Role}, CorrelationId: {CorrelationId}",
            userId, userRole, correlationId);

        var analytics = await _analyticsRepository.GetTransactionAnalyticsAsync();
        if (analytics == null)
        {
            _logger.LogError("Repository returned null for analytics overview by user {UserId}, CorrelationId: {CorrelationId}", userId, correlationId);
            var problemDetails = FinancialMonitoring.Models.ProblemDetails.InternalServerError("Failed to retrieve analytics.", HttpContext.Request.Path);
            var errorResponse = ApiErrorResponse.FromProblemDetails(problemDetails, correlationId);
            return StatusCode(500, errorResponse);
        }

        var response = ApiResponse<TransactionAnalytics>.SuccessResponse(analytics, correlationId);
        return Ok(response);
    }

    /// <summary>
    /// Gets time series data for transaction counts over a specified period (Admin and Analyst only).
    /// </summary>
    /// <param name="hours">Number of hours to look back from now.</param>
    /// <param name="intervalMinutes">Interval in minutes for data points (default: 60).</param>
    /// <returns>Time series data for transaction counts.</returns>
    [HttpGet("timeseries/transactions")]
    [Authorize(Roles = AppConstants.AdminAnalystRoles)]
    [Microsoft.AspNetCore.OutputCaching.OutputCache(PolicyName = AppConstants.TimeSeriesCachePolicy)]
    [ProducesResponseType(typeof(ApiResponse<List<TimeSeriesDataPoint>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<TimeSeriesDataPoint>>>> GetTransactionTimeSeries(
        [FromQuery, Range(1, 168)] int hours = 24,
        [FromQuery, Range(1, 1440)] int intervalMinutes = 60)
    {
        var correlationId = HttpContext.TraceIdentifier;
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        _logger.LogInformation("Secure transaction time series query by user {UserId} with role {Role} for {Hours} hours with {IntervalMinutes} minute intervals, CorrelationId: {CorrelationId}",
            userId, userRole, hours, intervalMinutes, correlationId);

        var toTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var fromTimestamp = DateTimeOffset.UtcNow.AddHours(-hours).ToUnixTimeMilliseconds();

        var timeSeries = await _analyticsRepository.GetTransactionTimeSeriesAsync(fromTimestamp, toTimestamp, intervalMinutes);
        if (timeSeries == null)
        {
            _logger.LogError("Repository returned null for transaction time series by user {UserId}, CorrelationId: {CorrelationId}", userId, correlationId);
            var problemDetails = FinancialMonitoring.Models.ProblemDetails.InternalServerError("Failed to retrieve transaction time series.", HttpContext.Request.Path);
            var errorResponse = ApiErrorResponse.FromProblemDetails(problemDetails, correlationId);
            return StatusCode(500, errorResponse);
        }

        var response = ApiResponse<List<TimeSeriesDataPoint>>.SuccessResponse(timeSeries, correlationId);
        return Ok(response);
    }

    /// <summary>
    /// Gets time series data for anomaly counts over a specified period (Admin and Analyst only).
    /// </summary>
    /// <param name="hours">Number of hours to look back from now.</param>
    /// <param name="intervalMinutes">Interval in minutes for data points (default: 60).</param>
    /// <returns>Time series data for anomaly counts.</returns>
    [HttpGet("timeseries/anomalies")]
    [Authorize(Roles = AppConstants.AdminAnalystRoles)]
    [Microsoft.AspNetCore.OutputCaching.OutputCache(PolicyName = AppConstants.TimeSeriesCachePolicy)]
    [ProducesResponseType(typeof(ApiResponse<List<TimeSeriesDataPoint>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<TimeSeriesDataPoint>>>> GetAnomalyTimeSeries(
        [FromQuery, Range(1, 168)] int hours = 24,
        [FromQuery, Range(1, 1440)] int intervalMinutes = 60)
    {
        var correlationId = HttpContext.TraceIdentifier;
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        _logger.LogInformation("Secure anomaly time series query by user {UserId} with role {Role} for {Hours} hours with {IntervalMinutes} minute intervals, CorrelationId: {CorrelationId}",
            userId, userRole, hours, intervalMinutes, correlationId);

        var toTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var fromTimestamp = DateTimeOffset.UtcNow.AddHours(-hours).ToUnixTimeMilliseconds();

        var timeSeries = await _analyticsRepository.GetAnomalyTimeSeriesAsync(fromTimestamp, toTimestamp, intervalMinutes);
        if (timeSeries == null)
        {
            _logger.LogError("Repository returned null for anomaly time series by user {UserId}, CorrelationId: {CorrelationId}", userId, correlationId);
            var problemDetails = FinancialMonitoring.Models.ProblemDetails.InternalServerError("Failed to retrieve anomaly time series.", HttpContext.Request.Path);
            var errorResponse = ApiErrorResponse.FromProblemDetails(problemDetails, correlationId);
            return StatusCode(500, errorResponse);
        }

        var response = ApiResponse<List<TimeSeriesDataPoint>>.SuccessResponse(timeSeries, correlationId);
        return Ok(response);
    }

    /// <summary>
    /// Gets analytics data for top merchants by transaction volume (Admin and Analyst only).
    /// </summary>
    /// <param name="count">Number of top merchants to return (default: 10).</param>
    /// <returns>Analytics data for top merchants.</returns>
    [HttpGet("merchants/top")]
    [Authorize(Roles = AppConstants.AdminAnalystRoles)]
    [Microsoft.AspNetCore.OutputCaching.OutputCache(PolicyName = AppConstants.AnalyticsCachePolicy)]
    [ProducesResponseType(typeof(ApiResponse<List<MerchantAnalytics>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<MerchantAnalytics>>>> GetTopMerchants(
        [FromQuery, Range(1, 50)] int count = 10)
    {
        var correlationId = HttpContext.TraceIdentifier;
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        _logger.LogInformation("Secure top merchants analytics query by user {UserId} with role {Role} for {Count} merchants, CorrelationId: {CorrelationId}",
            userId, userRole, count, correlationId);

        var merchantAnalytics = await _analyticsRepository.GetTopMerchantsAnalyticsAsync(count);
        if (merchantAnalytics == null)
        {
            _logger.LogError("Repository returned null for top merchants analytics by user {UserId}, CorrelationId: {CorrelationId}", userId, correlationId);
            var problemDetails = FinancialMonitoring.Models.ProblemDetails.InternalServerError("Failed to retrieve top merchants analytics.", HttpContext.Request.Path);
            var errorResponse = ApiErrorResponse.FromProblemDetails(problemDetails, correlationId);
            return StatusCode(500, errorResponse);
        }

        var response = ApiResponse<List<MerchantAnalytics>>.SuccessResponse(merchantAnalytics, correlationId);
        return Ok(response);
    }

    /// <summary>
    /// Gets analytics data grouped by merchant category (Admin and Analyst only).
    /// </summary>
    /// <returns>Analytics data grouped by merchant category.</returns>
    [HttpGet("merchants/categories")]
    [Authorize(Roles = AppConstants.AdminAnalystRoles)]
    [Microsoft.AspNetCore.OutputCaching.OutputCache(PolicyName = AppConstants.AnalyticsCachePolicy)]
    [ProducesResponseType(typeof(ApiResponse<List<MerchantAnalytics>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<MerchantAnalytics>>>> GetMerchantCategoryAnalytics()
    {
        var correlationId = HttpContext.TraceIdentifier;
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        _logger.LogInformation("Secure merchant category analytics query by user {UserId} with role {Role}, CorrelationId: {CorrelationId}",
            userId, userRole, correlationId);

        var categoryAnalytics = await _analyticsRepository.GetMerchantCategoryAnalyticsAsync();
        if (categoryAnalytics == null)
        {
            _logger.LogError("Repository returned null for merchant category analytics by user {UserId}, CorrelationId: {CorrelationId}", userId, correlationId);
            var problemDetails = FinancialMonitoring.Models.ProblemDetails.InternalServerError("Failed to retrieve merchant category analytics.", HttpContext.Request.Path);
            var errorResponse = ApiErrorResponse.FromProblemDetails(problemDetails, correlationId);
            return StatusCode(500, errorResponse);
        }

        var response = ApiResponse<List<MerchantAnalytics>>.SuccessResponse(categoryAnalytics, correlationId);
        return Ok(response);
    }

    /// <summary>
    /// Gets the current user's ID from the JWT claims.
    /// </summary>
    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
    }

    /// <summary>
    /// Gets the current user's role from the JWT claims.
    /// </summary>
    private string GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";
    }
}
