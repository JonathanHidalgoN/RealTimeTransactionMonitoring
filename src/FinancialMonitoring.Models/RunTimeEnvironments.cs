namespace FinancialMonitoring.Models;

/// <summary>
/// Represents the environments
/// </summary>
public enum RunTimeEnvironment
{
    Development,
    Testing,
    Production
}

/// <summary>
/// Extension methods for RunTimeEnvironment enum
/// </summary>
public static class RunTimeEnvironmentExtensions
{
    public static RunTimeEnvironment FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Environment string cannot be null or empty", nameof(value));

        if (Enum.TryParse<RunTimeEnvironment>(value, ignoreCase: true, out var result) &&
            Enum.IsDefined(typeof(RunTimeEnvironment), result))
        {
            return result;
        }

        var validOptions = string.Join(", ", Enum.GetNames<RunTimeEnvironment>());
        throw new ArgumentException($"Invalid environment: '{value}'. Valid options are: {validOptions}", nameof(value));
    }
}
