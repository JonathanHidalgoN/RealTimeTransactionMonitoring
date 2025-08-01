using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Api.Authentication;
using FinancialMonitoring.Api.Validation;
using FinancialMonitoring.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FinancialMonitoring.Api.Controllers;

/// <summary>
/// API endpoints for querying financial transaction data.
/// </summary>
[ApiController]
[ApiVersion(AppConstants.ApiVersion)]
[Route(AppConstants.TransactionsRouteTemplate)]
[Authorize(AuthenticationSchemes = SecureApiKeyAuthenticationDefaults.SchemeName)]
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
    /// Retrieves a paginated list of all transactions with optional filtering.
    /// </summary>
    /// <param name="request">The query parameters for filtering and pagination.</param>
    /// <returns>A paginated result of transactions.</returns>
    [HttpGet]
    [Microsoft.AspNetCore.OutputCaching.OutputCache(PolicyName = "TransactionCache")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<Transaction>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PagedResult<Transaction>>>> GetAllTransactions(
        [FromQuery] TransactionQueryRequest request)
    {
        var correlationId = HttpContext.TraceIdentifier;
        _logger.LogInformation("Getting all transactions - Page: {PageNumber}, Size: {PageSize}, CorrelationId: {CorrelationId}",
            request.PageNumber, request.PageSize, correlationId);

        var pagedResult = await _transactionRepository.GetAllTransactionsAsync(request.PageNumber, request.PageSize);
        var response = ApiResponse<PagedResult<Transaction>>.SuccessResponse(pagedResult, correlationId);

        return Ok(response);
    }

    /// <summary>
    /// Retrieves a specific transaction by its unique ID.
    /// </summary>
    /// <param name="id">The ID of the transaction to retrieve.</param>
    /// <returns>The requested transaction if found; otherwise, a 404 Not Found response.</returns>
    [HttpGet("{id}")]
    [Microsoft.AspNetCore.OutputCaching.OutputCache(PolicyName = "TransactionByIdCache")]
    [ProducesResponseType(typeof(ApiResponse<Transaction>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<Transaction>>> GetTransactionById([ValidTransactionId] string id)
    {
        var correlationId = HttpContext.TraceIdentifier;

        if (string.IsNullOrWhiteSpace(id))
        {
            _logger.LogWarning("Empty transaction ID provided for request {CorrelationId}", correlationId);
            var problemDetails = FinancialMonitoring.Models.ProblemDetails.ValidationError("Transaction ID cannot be empty.", HttpContext.Request.Path);
            var errorResponse = ApiErrorResponse.FromProblemDetails(problemDetails, correlationId);
            return BadRequest(errorResponse);
        }

        _logger.LogInformation("Getting transaction by ID: {TransactionId}, CorrelationId: {CorrelationId}", id, correlationId);

        var transaction = await _transactionRepository.GetTransactionByIdAsync(id);
        if (transaction == null)
        {
            _logger.LogWarning("Transaction with ID {TransactionId} not found for request {CorrelationId}", id, correlationId);
            var problemDetails = FinancialMonitoring.Models.ProblemDetails.NotFound($"Transaction with ID '{id}' was not found.", HttpContext.Request.Path);
            var errorResponse = ApiErrorResponse.FromProblemDetails(problemDetails, correlationId);
            return NotFound(errorResponse);
        }

        var response = ApiResponse<Transaction>.SuccessResponse(transaction, correlationId);
        return Ok(response);
    }

    /// <summary>
    /// Retrieves a paginated list of transactions that have been flagged as anomalous.
    /// </summary>
    /// <param name="request">The query parameters for filtering and pagination.</param>
    /// <returns>A paginated result of anomalous transactions.</returns>
    [HttpGet("anomalies")]
    [Microsoft.AspNetCore.OutputCaching.OutputCache(PolicyName = "AnomalousTransactionCache")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<Transaction>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<PagedResult<Transaction>>>> GetAnomalousTransactions(
        [FromQuery] TransactionQueryRequest request)
    {
        var correlationId = HttpContext.TraceIdentifier;
        _logger.LogInformation("Getting anomalous transactions - Page: {PageNumber}, Size: {PageSize}, CorrelationId: {CorrelationId}",
            request.PageNumber, request.PageSize, correlationId);

        var pagedResult = await _transactionRepository.GetAnomalousTransactionsAsync(request.PageNumber, request.PageSize);
        var response = ApiResponse<PagedResult<Transaction>>.SuccessResponse(pagedResult, correlationId);

        return Ok(response);
    }
}
