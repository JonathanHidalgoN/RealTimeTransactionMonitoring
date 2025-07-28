using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Api.Authentication;
using FinancialMonitoring.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FinancialMonitoring.Api.Controllers;

/// <summary>
/// API endpoints for querying financial transaction data.
/// </summary>
[ApiController]
[Route("api/transactions")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.SchemeName)]
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
    /// Retrieves a paginated list of all transactions.
    /// </summary>
    /// <param name="pageNumber">The page number to retrieve.</param>
    /// <param name="pageSize">The number of transactions per page.</param>
    /// <returns>A paginated result of transactions.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Transaction>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<Transaction>>> GetAllTransactions(
        [FromQuery][Range(1, int.MaxValue)] int pageNumber = 1,
        [FromQuery][Range(1, 100)] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("API: Getting all transactions - Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);
            var pagedResult = await _transactionRepository.GetAllTransactionsAsync(pageNumber, pageSize);
            return Ok(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error occurred while getting all transactions.");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
        }
    }

    /// <summary>
    /// Retrieves a specific transaction by its unique ID.
    /// </summary>
    /// <param name="id">The ID of the transaction to retrieve.</param>
    /// <returns>The requested transaction if found; otherwise, a 404 Not Found response.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Transaction), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Transaction>> GetTransactionById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("Transaction ID cannot be empty.");
        }

        try
        {
            _logger.LogInformation("API: Getting transaction by ID: {TransactionId}", id);
            var transaction = await _transactionRepository.GetTransactionByIdAsync(id);
            if (transaction == null)
            {
                _logger.LogWarning("API: Transaction with ID {TransactionId} not found.", id);
                return NotFound();
            }
            return Ok(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error occurred while getting transaction by ID: {TransactionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
        }
    }

    /// <summary>
    /// Retrieves a paginated list of transactions that have been flagged as anomalous.
    /// </summary>
    /// <param name="pageNumber">The page number to retrieve.</param>
    /// <param name="pageSize">The number of transactions per page.</param>
    /// <returns>A paginated result of anomalous transactions.</returns>
    [HttpGet("anomalies")]
    [ProducesResponseType(typeof(IEnumerable<Transaction>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<Transaction>>> GetAnomalousTransactions(
        [FromQuery][Range(1, int.MaxValue)] int pageNumber = 1,
        [FromQuery][Range(1, 100)] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("API: Getting anomalous transactions - Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);
            var transactions = await _transactionRepository.GetAnomalousTransactionsAsync(pageNumber, pageSize);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error occurred while getting anomalous transactions.");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
        }
    }
}
