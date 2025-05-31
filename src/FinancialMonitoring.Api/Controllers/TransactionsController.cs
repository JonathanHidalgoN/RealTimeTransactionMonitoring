using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FinancialMonitoring.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionQueryService _queryService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(ITransactionQueryService queryService, ILogger<TransactionsController> logger)
    {
        _queryService = queryService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Transaction>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<Transaction>>> GetAllTransactions(
        [FromQuery][Range(1, int.MaxValue)] int pageNumber = 1,
        [FromQuery][Range(1, 100)] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("API: Getting all transactions - Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);
            var transactions = await _queryService.GetAllTransactionsAsync(pageNumber, pageSize);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error occurred while getting all transactions.");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
        }
    }

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
            var transaction = await _queryService.GetTransactionByIdAsync(id);
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
            var transactions = await _queryService.GetAnomalousTransactionsAsync(pageNumber, pageSize);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API: Error occurred while getting anomalous transactions.");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
        }
    }
}