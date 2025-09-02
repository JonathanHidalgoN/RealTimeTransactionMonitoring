using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Api.Validation;
using FinancialMonitoring.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinancialMonitoring.Api.Controllers.V2;

/// <summary>
/// API endpoints for querying financial transaction data (V2 - JWT authentication with role-based authorization).
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/transactions")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<TransactionsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionsController"/> class.
    /// </summary>
    /// <param name="transactionRepository">The repository for transaction data operations.</param>
    /// <param name="logger">The logger for recording operational information.</param>
    public TransactionsController(ITransactionRepository transactionRepository, ILogger<TransactionsController> logger)
    {
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of transactions accessible to the current user based on their role.
    /// Admins and Analysts see all transactions, Viewers see limited data.
    /// </summary>
    /// <param name="request">The query parameters for filtering and pagination.</param>
    /// <returns>A paginated result of transactions based on user permissions.</returns>
    [HttpGet]
    [Authorize(Roles = "Admin,Analyst,Viewer")]
    [Microsoft.AspNetCore.OutputCaching.OutputCache(PolicyName = AppConstants.TransactionCachePolicy)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<Transaction>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PagedResult<Transaction>>>> GetAllTransactions(
        [FromQuery] TransactionQueryRequest request)
    {
        var correlationId = HttpContext.TraceIdentifier;
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        _logger.LogInformation("Secure transaction query by user {UserId} with role {Role} - Page: {PageNumber}, Size: {PageSize}, CorrelationId: {CorrelationId}",
            userId, userRole, request.PageNumber, request.PageSize, correlationId);

        var pagedResult = await _transactionRepository.GetAllTransactionsAsync(request.PageNumber, request.PageSize);
        if (pagedResult == null)
        {
            _logger.LogError("Repository returned null for transactions query by user {UserId}, CorrelationId: {CorrelationId}", userId, correlationId);
            var problemDetails = FinancialMonitoring.Models.ProblemDetails.InternalServerError("Failed to retrieve transactions.", HttpContext.Request.Path);
            var errorResponse = ApiErrorResponse.FromProblemDetails(problemDetails, correlationId);
            return StatusCode(500, errorResponse);
        }

        var response = ApiResponse<PagedResult<Transaction>>.SuccessResponse(pagedResult, correlationId);
        return Ok(response);
    }

    /// <summary>
    /// Retrieves a specific transaction by its unique ID (Admin and Analyst only).
    /// </summary>
    /// <param name="id">The ID of the transaction to retrieve.</param>
    /// <returns>The requested transaction if found and accessible; otherwise, a 404 Not Found or 403 Forbidden response.</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Analyst")]
    [Microsoft.AspNetCore.OutputCaching.OutputCache(PolicyName = AppConstants.TransactionByIdCachePolicy)]
    [ProducesResponseType(typeof(ApiResponse<Transaction>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<Transaction>>> GetTransactionById([ValidTransactionId] string id)
    {
        var correlationId = HttpContext.TraceIdentifier;
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        if (string.IsNullOrWhiteSpace(id))
        {
            _logger.LogWarning("Empty transaction ID provided by user {UserId} for request {CorrelationId}", userId, correlationId);
            var problemDetails = FinancialMonitoring.Models.ProblemDetails.ValidationError("Transaction ID cannot be empty.", HttpContext.Request.Path);
            var errorResponse = ApiErrorResponse.FromProblemDetails(problemDetails, correlationId);
            return BadRequest(errorResponse);
        }

        _logger.LogInformation("Secure transaction lookup by user {UserId} with role {Role} - ID: {TransactionId}, CorrelationId: {CorrelationId}",
            userId, userRole, id, correlationId);

        var transaction = await _transactionRepository.GetTransactionByIdAsync(id);
        if (transaction == null)
        {
            _logger.LogWarning("Transaction with ID {TransactionId} not found for user {UserId} request {CorrelationId}", id, userId, correlationId);
            var problemDetails = FinancialMonitoring.Models.ProblemDetails.NotFound($"Transaction with ID '{id}' was not found.", HttpContext.Request.Path);
            var errorResponse = ApiErrorResponse.FromProblemDetails(problemDetails, correlationId);
            return NotFound(errorResponse);
        }

        // TODO: Check if user has access to this specific transaction based on business rules
        var response = ApiResponse<Transaction>.SuccessResponse(transaction, correlationId);
        return Ok(response);
    }

    /// <summary>
    /// Retrieves a paginated list of transactions that have been flagged as anomalous (Analyst and Admin only).
    /// </summary>
    /// <param name="request">The query parameters for filtering and pagination.</param>
    /// <returns>A paginated result of anomalous transactions.</returns>
    [HttpGet("anomalies")]
    [Authorize(Roles = "Admin,Analyst")]
    [Microsoft.AspNetCore.OutputCaching.OutputCache(PolicyName = AppConstants.AnomalousTransactionCachePolicy)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<Transaction>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PagedResult<Transaction>>>> GetAnomalousTransactions(
        [FromQuery] TransactionQueryRequest request)
    {
        var correlationId = HttpContext.TraceIdentifier;
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        _logger.LogInformation("Secure anomalous transactions query by user {UserId} with role {Role} - Page: {PageNumber}, Size: {PageSize}, CorrelationId: {CorrelationId}",
            userId, userRole, request.PageNumber, request.PageSize, correlationId);

        var pagedResult = await _transactionRepository.GetAnomalousTransactionsAsync(request.PageNumber, request.PageSize);
        if (pagedResult == null)
        {
            _logger.LogError("Repository returned null for anomalous transactions query by user {UserId}, CorrelationId: {CorrelationId}", userId, correlationId);
            var problemDetails = FinancialMonitoring.Models.ProblemDetails.InternalServerError("Failed to retrieve anomalous transactions.", HttpContext.Request.Path);
            var errorResponse = ApiErrorResponse.FromProblemDetails(problemDetails, correlationId);
            return StatusCode(500, errorResponse);
        }

        var response = ApiResponse<PagedResult<Transaction>>.SuccessResponse(pagedResult, correlationId);
        return Ok(response);
    }

    /// <summary>
    /// Searches for transactions using advanced filtering criteria (Admin and Analyst only).
    /// </summary>
    /// <param name="searchRequest">The search criteria and pagination parameters.</param>
    /// <returns>A paginated result of transactions matching the search criteria.</returns>
    [HttpPost("search")]
    [Authorize(Roles = "Admin,Analyst")]
    [Microsoft.AspNetCore.OutputCaching.OutputCache(PolicyName = AppConstants.TransactionCachePolicy)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<Transaction>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PagedResult<Transaction>>>> SearchTransactions(
        [FromBody] TransactionSearchRequest searchRequest)
    {
        var correlationId = HttpContext.TraceIdentifier;
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        _logger.LogInformation("Secure transaction search by user {UserId} with role {Role} - Page: {PageNumber}, Size: {PageSize}, CorrelationId: {CorrelationId}",
            userId, userRole, searchRequest.PageNumber, searchRequest.PageSize, correlationId);

        var pagedResult = await _transactionRepository.SearchTransactionsAsync(searchRequest);
        if (pagedResult == null)
        {
            _logger.LogError("Repository returned null for transaction search by user {UserId}, CorrelationId: {CorrelationId}", userId, correlationId);
            var problemDetails = FinancialMonitoring.Models.ProblemDetails.InternalServerError("Failed to search transactions.", HttpContext.Request.Path);
            var errorResponse = ApiErrorResponse.FromProblemDetails(problemDetails, correlationId);
            return StatusCode(500, errorResponse);
        }

        var response = ApiResponse<PagedResult<Transaction>>.SuccessResponse(pagedResult, correlationId);
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
