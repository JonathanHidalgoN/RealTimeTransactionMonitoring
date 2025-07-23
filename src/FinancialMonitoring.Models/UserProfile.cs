namespace FinancialMonitoring.Models;

/// <summary>
/// Represents a user profile with realistic spending behaviors
/// </summary>
public record UserProfile
{
    /// <summary>
    /// The account ID associated with this profile
    /// </summary>
    public string AccountId { get; init; }

    /// <summary>
    /// The type of user (Student, Professional, etc.)
    /// </summary>
    public UserType Type { get; init; }

    /// <summary>
    /// Monthly income in the account's currency
    /// </summary>
    public decimal MonthlyIncome { get; init; }

    /// <summary>
    /// Primary location where the user lives/works
    /// </summary>
    public Location HomeLocation { get; init; }

    /// <summary>
    /// Time zone for the user (hours offset from UTC)
    /// </summary>
    public int TimeZoneOffset { get; init; }

    /// <summary>
    /// List of spending patterns for different categories
    /// </summary>
    public List<SpendingPattern> SpendingPatterns { get; init; }

    /// <summary>
    /// Risk tolerance for generating anomalous transactions (0.0 to 1.0)
    /// Higher values mean more likely to have unusual transactions
    /// </summary>
    public double RiskTolerance { get; init; }

    /// <summary>
    /// Probability of traveling (generating transactions in different locations)
    /// </summary>
    public double TravelProbability { get; init; }

    public UserProfile(
        string accountId,
        UserType type,
        decimal monthlyIncome,
        Location homeLocation,
        int timeZoneOffset = 0,
        List<SpendingPattern>? spendingPatterns = null,
        double riskTolerance = 0.1,
        double travelProbability = 0.05)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            throw new ArgumentException("Account ID cannot be null or whitespace.", nameof(accountId));
        if (monthlyIncome < 0)
            throw new ArgumentOutOfRangeException(nameof(monthlyIncome), "Monthly income cannot be negative.");
        if (timeZoneOffset < -12 || timeZoneOffset > 14)
            throw new ArgumentOutOfRangeException(nameof(timeZoneOffset), "Time zone offset must be between -12 and +14.");
        if (riskTolerance < 0.0 || riskTolerance > 1.0)
            throw new ArgumentOutOfRangeException(nameof(riskTolerance), "Risk tolerance must be between 0.0 and 1.0.");
        if (travelProbability < 0.0 || travelProbability > 1.0)
            throw new ArgumentOutOfRangeException(nameof(travelProbability), "Travel probability must be between 0.0 and 1.0.");

        ArgumentNullException.ThrowIfNull(homeLocation);

        AccountId = accountId;
        Type = type;
        MonthlyIncome = monthlyIncome;
        HomeLocation = homeLocation;
        TimeZoneOffset = timeZoneOffset;
        SpendingPatterns = spendingPatterns ?? new List<SpendingPattern>();
        RiskTolerance = riskTolerance;
        TravelProbability = travelProbability;
    }
}
