using Microsoft.AspNetCore.OutputCaching;

namespace FinancialMonitoring.Api.Extensions;

/// <summary>
/// Extension methods for OutputCachePolicyBuilder to reduce configuration boilerplate
/// </summary>
public static class OutputCachePolicyBuilderExtensions
{
    /// <summary>
    /// Conditionally applies caching based on duration in seconds.
    /// If seconds greater than 0, applies the specified duration and optional configuration.
    /// If seconds less than or equal to 0, disables caching with NoCache().
    /// </summary>
    /// <param name="builder">The policy builder</param>
    /// <param name="seconds">Cache duration in seconds. 0 or negative disables caching.</param>
    /// <param name="configurePolicy">Optional additional configuration (vary-by rules, etc.)</param>
    /// <returns>The policy builder for method chaining</returns>
    public static OutputCachePolicyBuilder ConditionalExpire(
        this OutputCachePolicyBuilder builder, 
        int seconds,
        Action<OutputCachePolicyBuilder>? configurePolicy = null)
    {
        if (seconds > 0)
        {
            builder.Expire(TimeSpan.FromSeconds(seconds));
            configurePolicy?.Invoke(builder);
        }
        else
        {
            builder.NoCache();
        }
        return builder;
    }
}