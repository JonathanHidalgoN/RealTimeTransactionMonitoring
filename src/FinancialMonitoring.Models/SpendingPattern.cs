namespace FinancialMonitoring.Models;

/// <summary>
/// Represents a spending pattern for a specific merchant category
/// </summary>
public record SpendingPattern
{
    /// <summary>
    /// The merchant category this pattern applies to
    /// </summary>
    public MerchantCategory Category { get; init; }

    /// <summary>
    /// Average amount spent per transaction in this category
    /// </summary>
    public double AverageAmount { get; init; }

    /// <summary>
    /// Standard deviation for amount variation
    /// </summary>
    public double AmountStdDev { get; init; }

    /// <summary>
    /// Average number of transactions per month in this category
    /// </summary>
    public int MonthlyFrequency { get; init; }

    /// <summary>
    /// Preferred time of day for transactions (0-23)
    /// </summary>
    public int PreferredHour { get; init; }

    /// <summary>
    /// Probability of transaction on weekends (0.0 to 1.0)
    /// </summary>
    public double WeekendProbability { get; init; }

    public SpendingPattern(
        MerchantCategory category,
        double averageAmount,
        double amountStdDev,
        int monthlyFrequency,
        int preferredHour = 12,
        double weekendProbability = 0.3)
    {
        if (averageAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(averageAmount), "Average amount must be positive.");
        if (amountStdDev < 0)
            throw new ArgumentOutOfRangeException(nameof(amountStdDev), "Standard deviation cannot be negative.");
        if (monthlyFrequency < 0)
            throw new ArgumentOutOfRangeException(nameof(monthlyFrequency), "Monthly frequency cannot be negative.");
        if (preferredHour < 0 || preferredHour > 23)
            throw new ArgumentOutOfRangeException(nameof(preferredHour), "Preferred hour must be between 0 and 23.");
        if (weekendProbability < 0.0 || weekendProbability > 1.0)
            throw new ArgumentOutOfRangeException(nameof(weekendProbability), "Weekend probability must be between 0.0 and 1.0.");

        Category = category;
        AverageAmount = averageAmount;
        AmountStdDev = amountStdDev;
        MonthlyFrequency = monthlyFrequency;
        PreferredHour = preferredHour;
        WeekendProbability = weekendProbability;
    }
}
