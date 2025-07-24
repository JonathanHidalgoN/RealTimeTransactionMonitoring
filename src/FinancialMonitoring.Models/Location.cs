namespace FinancialMonitoring.Models;

/// <summary>
/// Represents a geographic location for a transaction
/// </summary>
public record Location
{
    /// <summary>
    /// City name
    /// </summary>
    public string City { get; init; }

    /// <summary>
    /// State or province code
    /// </summary>
    public string State { get; init; }

    /// <summary>
    /// Country code
    /// </summary>
    public string Country { get; init; }

    /// <summary>
    /// ZIP or postal code
    /// </summary>
    public string? PostalCode { get; init; }

    /// <summary>
    /// Latitude coordinate
    /// </summary>
    public double? Latitude { get; init; }

    /// <summary>
    /// Longitude coordinate
    /// </summary>
    public double? Longitude { get; init; }

    public Location(string city, string state, string country, string? postalCode = null, double? latitude = null, double? longitude = null)
    {
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City cannot be null or whitespace.", nameof(city));
        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("State cannot be null or whitespace.", nameof(state));
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country cannot be null or whitespace.", nameof(country));

        City = city;
        State = state;
        Country = country;
        PostalCode = postalCode;
        Latitude = latitude;
        Longitude = longitude;
    }

    public override string ToString()
    {
        return $"{City}, {State}, {Country}";
    }
}
