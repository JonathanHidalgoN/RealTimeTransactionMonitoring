using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FinancialMonitoring.Models;

/// <summary>
/// Request model for advanced transaction search functionality.
/// </summary>
public record TransactionSearchRequest
{
    /// <summary>
    /// Page number for pagination (1-based).
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0.")]
    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Number of items per page.
    /// </summary>
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100.")]
    [JsonPropertyName("pageSize")]
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Filter transactions from this timestamp (Unix milliseconds).
    /// </summary>
    [JsonPropertyName("fromTimestamp")]
    public long? FromTimestamp { get; init; }

    /// <summary>
    /// Filter transactions to this timestamp (Unix milliseconds).
    /// </summary>
    [JsonPropertyName("toTimestamp")]
    public long? ToTimestamp { get; init; }

    /// <summary>
    /// Minimum transaction amount filter.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Minimum amount must be non-negative.")]
    [JsonPropertyName("minAmount")]
    public double? MinAmount { get; init; }

    /// <summary>
    /// Maximum transaction amount filter.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Maximum amount must be non-negative.")]
    [JsonPropertyName("maxAmount")]
    public double? MaxAmount { get; init; }

    /// <summary>
    /// Filter by specific merchant category.
    /// </summary>
    [JsonPropertyName("merchantCategory")]
    public MerchantCategory? MerchantCategory { get; init; }

    /// <summary>
    /// Filter by merchant name (partial match).
    /// </summary>
    [JsonPropertyName("merchantName")]
    public string? MerchantName { get; init; }

    /// <summary>
    /// Filter by specific payment method.
    /// </summary>
    [JsonPropertyName("paymentMethod")]
    public PaymentMethod? PaymentMethod { get; init; }

    /// <summary>
    /// Filter by specific source account ID.
    /// </summary>
    [JsonPropertyName("sourceAccountId")]
    public string? SourceAccountId { get; init; }

    /// <summary>
    /// Filter by specific destination account ID.
    /// </summary>
    [JsonPropertyName("destinationAccountId")]
    public string? DestinationAccountId { get; init; }

    /// <summary>
    /// Filter by transaction type.
    /// </summary>
    [JsonPropertyName("transactionType")]
    public TransactionType? TransactionType { get; init; }

    /// <summary>
    /// If true, only return anomalous transactions.
    /// </summary>
    [JsonPropertyName("anomaliesOnly")]
    public bool AnomaliesOnly { get; init; } = false;

    /// <summary>
    /// Filter by specific anomaly flag.
    /// </summary>
    [JsonPropertyName("anomalyFlag")]
    public string? AnomalyFlag { get; init; }

    /// <summary>
    /// Filter by location city.
    /// </summary>
    [JsonPropertyName("city")]
    public string? City { get; init; }

    /// <summary>
    /// Filter by location state.
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; init; }

    /// <summary>
    /// Sort field (amount, timestamp, etc.).
    /// </summary>
    [JsonPropertyName("sortBy")]
    public string? SortBy { get; init; }

    /// <summary>
    /// Sort direction (asc, desc).
    /// </summary>
    [JsonPropertyName("sortDirection")]
    public string? SortDirection { get; init; } = "desc";
}