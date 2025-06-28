namespace FinancialMonitoring.Models;

/// <summary>
/// Represents a financial account.
/// </summary>
/// <remarks>
/// This record ensures that an account has a valid, non-empty identifier upon creation.
/// </remarks>
public record Account
{
    /// <summary>
    /// The unique identifier for the account.
    /// </summary>
    public string AccountId { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Account"/> class.
    /// </summary>
    /// <param name="accountId">The unique identifier for the account.</param>
    /// <exception cref="ArgumentException">Thrown if the account ID is null or whitespace.</exception>
    public Account(string accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException("Account ID cannot be null or whitespace.", nameof(accountId));
        }
        AccountId = accountId;
    }
}
