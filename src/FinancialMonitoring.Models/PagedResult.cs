namespace FinancialMonitoring.Models;

/// <summary>
/// Represents a single page of results from a paginated query.
/// </summary>
/// <typeparam name="T">The type of the items in the page.</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// The items for the current page.
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// The total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// The current page number.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; set; }
}
