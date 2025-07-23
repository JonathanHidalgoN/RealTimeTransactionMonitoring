using FinancialMonitoring.Models;

namespace TransactionSimulator.Data;

/// <summary>
/// Static data for realistic merchant names by category
/// </summary>
public static class MerchantData
{
    public static readonly Dictionary<MerchantCategory, List<string>> MerchantsByCategory = new()
    {
        [MerchantCategory.Grocery] = new List<string>
        {
            "Walmart Supercenter", "Target", "Kroger", "Safeway", "Whole Foods Market",
            "Costco Wholesale", "Trader Joe's", "Publix", "H-E-B", "Meijer"
        },
        [MerchantCategory.Restaurant] = new List<string>
        {
            "McDonald's", "Starbucks", "Subway", "Pizza Hut", "KFC",
            "Taco Bell", "Burger King", "Domino's Pizza", "Chipotle", "Wendy's"
        },
        [MerchantCategory.Gas] = new List<string>
        {
            "Shell", "ExxonMobil", "BP", "Chevron", "Texaco",
            "Sunoco", "Marathon", "Citgo", "Phillips 66", "Valero"
        },
        [MerchantCategory.Retail] = new List<string>
        {
            "Amazon", "Best Buy", "Home Depot", "Lowe's", "Macy's",
            "Target", "Walmart", "Kohl's", "JCPenney", "Nordstrom"
        },
        [MerchantCategory.Healthcare] = new List<string>
        {
            "CVS Pharmacy", "Walgreens", "Rite Aid", "Kaiser Permanente", "Mayo Clinic",
            "Cleveland Clinic", "Johns Hopkins", "Mass General", "UCLA Medical", "NYU Langone"
        },
        [MerchantCategory.Entertainment] = new List<string>
        {
            "Netflix", "Disney+", "HBO Max", "AMC Theatres", "Regal Cinemas",
            "Spotify", "Apple Music", "YouTube Premium", "Xbox Live", "PlayStation Store"
        },
        [MerchantCategory.Travel] = new List<string>
        {
            "Expedia", "Booking.com", "Airbnb", "Delta Airlines", "American Airlines",
            "United Airlines", "Southwest Airlines", "Marriott", "Hilton", "Hyatt"
        },
        [MerchantCategory.Utilities] = new List<string>
        {
            "ConEd", "PG&E", "Duke Energy", "Southern Company", "Verizon",
            "AT&T", "Comcast", "Spectrum", "T-Mobile", "Sprint"
        },
        [MerchantCategory.OnlineServices] = new List<string>
        {
            "Amazon Web Services", "Google Cloud", "Microsoft Azure", "PayPal", "Stripe",
            "Square", "Shopify", "Adobe Creative", "Zoom", "Slack"
        },
        [MerchantCategory.ATM] = new List<string>
        {
            "Chase ATM", "Bank of America ATM", "Wells Fargo ATM", "Citibank ATM", "PNC ATM",
            "US Bank ATM", "TD Bank ATM", "Capital One ATM", "SunTrust ATM", "BB&T ATM"
        }
    };

    public static string GetRandomMerchant(MerchantCategory category, Random random)
    {
        if (MerchantsByCategory.TryGetValue(category, out var merchants))
        {
            return merchants[random.Next(merchants.Count)];
        }
        return $"Generic {category} Merchant";
    }
}