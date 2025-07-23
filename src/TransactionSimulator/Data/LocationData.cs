using FinancialMonitoring.Models;

namespace TransactionSimulator.Data;

/// <summary>
/// Static data for realistic US locations
/// </summary>
public static class LocationData
{
    public static readonly List<Location> UsLocations = new()
    {
        new Location("New York", "NY", "US", "10001", 40.7580, -73.9855),
        new Location("Los Angeles", "CA", "US", "90210", 34.0522, -118.2437),
        new Location("Chicago", "IL", "US", "60601", 41.8781, -87.6298),
        new Location("Houston", "TX", "US", "77001", 29.7604, -95.3698),
        new Location("Phoenix", "AZ", "US", "85001", 33.4484, -112.0740),
        new Location("Philadelphia", "PA", "US", "19101", 39.9526, -75.1652),
        new Location("San Antonio", "TX", "US", "78201", 29.4241, -98.4936),
        new Location("San Diego", "CA", "US", "92101", 32.7157, -117.1611),
        new Location("Dallas", "TX", "US", "75201", 32.7767, -96.7970),
        new Location("San Jose", "CA", "US", "95101", 37.3382, -121.8863),
        new Location("Austin", "TX", "US", "73301", 30.2672, -97.7431),
        new Location("Jacksonville", "FL", "US", "32099", 30.3322, -81.6557),
        new Location("Fort Worth", "TX", "US", "76101", 32.7555, -97.3308),
        new Location("Columbus", "OH", "US", "43085", 39.9612, -82.9988),
        new Location("Charlotte", "NC", "US", "28202", 35.2271, -80.8431),
        new Location("San Francisco", "CA", "US", "94102", 37.7749, -122.4194),
        new Location("Indianapolis", "IN", "US", "46201", 39.7684, -86.1581),
        new Location("Seattle", "WA", "US", "98101", 47.6062, -122.3321),
        new Location("Denver", "CO", "US", "80202", 39.7392, -104.9903),
        new Location("Boston", "MA", "US", "02101", 42.3601, -71.0589),
        new Location("Nashville", "TN", "US", "37201", 36.1627, -86.7816),
        new Location("Oklahoma City", "OK", "US", "73102", 35.4676, -97.5164),
        new Location("Las Vegas", "NV", "US", "89101", 36.1699, -115.1398),
        new Location("Portland", "OR", "US", "97201", 45.5152, -122.6784),
        new Location("Memphis", "TN", "US", "38103", 35.1495, -90.0490),
        new Location("Louisville", "KY", "US", "40202", 38.2527, -85.7585),
        new Location("Baltimore", "MD", "US", "21201", 39.2904, -76.6122),
        new Location("Milwaukee", "WI", "US", "53202", 43.0389, -87.9065),
        new Location("Albuquerque", "NM", "US", "87102", 35.0853, -106.6056),
        new Location("Tucson", "AZ", "US", "85701", 32.2226, -110.9747),
        new Location("Fresno", "CA", "US", "93701", 36.7378, -119.7871),
        new Location("Sacramento", "CA", "US", "95814", 38.5816, -121.4944),
        new Location("Mesa", "AZ", "US", "85201", 33.4152, -111.8315),
        new Location("Kansas City", "MO", "US", "64108", 39.0997, -94.5786),
        new Location("Atlanta", "GA", "US", "30303", 33.7490, -84.3880),
        new Location("Colorado Springs", "CO", "US", "80903", 38.8339, -104.8214),
        new Location("Raleigh", "NC", "US", "27601", 35.7796, -78.6382),
        new Location("Omaha", "NE", "US", "68102", 41.2565, -95.9345),
        new Location("Miami", "FL", "US", "33101", 25.7617, -80.1918),
        new Location("Long Beach", "CA", "US", "90802", 33.7701, -118.1937)
    };

    public static Location GetRandomLocation(Random random)
    {
        return UsLocations[random.Next(UsLocations.Count)];
    }

    public static Location GetNearbyLocation(Location homeLocation, Random random, double maxDistanceKm = 50.0)
    {
        // For simplicity, just return a random location
        var nearbyLocations = UsLocations
            .Where(loc => loc.State == homeLocation.State)
            .ToList();

        if (nearbyLocations.Any())
        {
            return nearbyLocations[random.Next(nearbyLocations.Count)];
        }

        return homeLocation;
    }
}
