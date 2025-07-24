using FinancialMonitoring.Models;
using TransactionSimulator.Data;

namespace TransactionSimulator.Generation;

/// <summary>
/// Professional-grade transaction generator with realistic patterns
/// </summary>
public class TransactionGenerator
{
    private const int DAYS_IN_MONTH = 30;
    private const int HOURS_IN_HALF_DAY = 12;
    private const int MINUTE_OFFSET_RANGE = 30;
    private const int USER_PROFILE_COUNT = 100;
    private const double WEEKEND_ACTIVITY_MULTIPLIER = 0.7;
    private const double MINIMUM_WEIGHT = 0.1;
    private const double MINIMUM_AMOUNT = 0.01;
    private const double LARGE_AMOUNT_THRESHOLD = 1000.0;
    private const double DIGITAL_WALLET_PROBABILITY = 0.4;
    private const double DEBIT_CARD_PROBABILITY = 0.6;
    private const double CHECK_PROBABILITY = 0.3;
    private const int ACCOUNT_ID_BASE = 1000;
    private const int MERCHANT_ID_MIN = 1000;
    private const int MERCHANT_ID_MAX = 9999;

    private readonly Random _random;
    private readonly List<UserProfile> _userProfiles;
    private readonly Dictionary<MerchantCategory, (double min, double max)> _categoryAmountRanges;

    public TransactionGenerator(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        _userProfiles = GenerateUserProfiles();
        _categoryAmountRanges = InitializeCategoryAmountRanges();
    }

    /// <summary>
    /// Generates a realistic transaction with proper temporal, geographic, and behavioral patterns
    /// </summary>
    public Transaction GenerateRealisticTransaction()
    {
        var userProfile = _userProfiles[_random.Next(_userProfiles.Count)];
        var now = DateTimeOffset.UtcNow;
        var userLocalTime = now.AddHours(userProfile.TimeZoneOffset);
        var adjustedTime = GetRealisticTransactionTime(userLocalTime, userProfile);
        var (category, pattern) = SelectCategoryAndPattern(userProfile, adjustedTime);
        var transactionType = GetTransactionTypeForCategory(category);
        var amount = GenerateRealisticAmount(pattern, category);
        var merchantName = MerchantData.GetRandomMerchant(category, _random);
        var location = GetTransactionLocation(userProfile, adjustedTime);
        var paymentMethod = GetRealisticPaymentMethod(category, amount);

        return new Transaction(
            id: Guid.NewGuid().ToString(),
            amount: Math.Round(amount, 2),
            timestamp: adjustedTime.ToUnixTimeMilliseconds(),
            sourceAccount: new Account(userProfile.AccountId),
            destinationAccount: GenerateDestinationAccount(category, merchantName),
            type: transactionType,
            merchantCategory: category,
            merchantName: merchantName,
            location: location,
            paymentMethod: paymentMethod
        );
    }

    /// <summary>
    /// Adjusts transaction time based on realistic activity patterns, hour weights, and user profile characteristics
    /// </summary>
    private DateTimeOffset GetRealisticTransactionTime(DateTimeOffset userLocalTime, UserProfile profile)
    {
        var hour = userLocalTime.Hour;
        var baseHourWeight = GetHourWeight(hour);
        var isWeekend = userLocalTime.DayOfWeek == DayOfWeek.Saturday || userLocalTime.DayOfWeek == DayOfWeek.Sunday;
        
        var hourWeight = isWeekend ? baseHourWeight * WEEKEND_ACTIVITY_MULTIPLIER : baseHourWeight;
        
        hourWeight = ApplyUserTypeHourAdjustment(hourWeight, hour, profile.Type, isWeekend);
        
        if (_random.NextDouble() > hourWeight)
        {
            var activeHours = GetActiveHoursForUserType(profile.Type, isWeekend);
            var newHour = activeHours[_random.Next(activeHours.Length)];
            userLocalTime = userLocalTime.Date.AddHours(newHour).AddMinutes(_random.Next(0, 60));
        }

        var minuteOffset = _random.Next(-MINUTE_OFFSET_RANGE, MINUTE_OFFSET_RANGE + 1);
        return userLocalTime.AddMinutes(minuteOffset);
    }

    /// <summary>
    /// Returns activity weight based on hour of day (0.2-1.0)
    /// </summary>
    private double GetHourWeight(int hour)
    {
        if (hour >= 9 && hour <= 17) return 1.0;
        if (hour >= 18 && hour <= 22) return 0.8;
        if (hour >= 6 && hour <= 8) return 0.6;
        return 0.2;
    }

    /// <summary>
    /// Adjusts hour weight based on user type and behavioral patterns
    /// </summary>
    private double ApplyUserTypeHourAdjustment(double baseWeight, int hour, UserType userType, bool isWeekend)
    {
        return userType switch
        {
            UserType.Student => hour >= 20 && hour <= 23 ? baseWeight * 1.3 : baseWeight,
            UserType.YoungProfessional => hour >= 18 && hour <= 20 ? baseWeight * 1.2 : baseWeight,
            UserType.Retiree => hour >= 10 && hour <= 16 ? baseWeight * 1.1 : baseWeight,
            UserType.SmallBusiness => hour >= 6 && hour <= 8 || hour >= 17 && hour <= 19 ? baseWeight * 1.2 : baseWeight,
            UserType.Freelancer => isWeekend ? baseWeight * 1.1 : baseWeight,
            UserType.HighNetWorth => hour >= 10 && hour <= 16 ? baseWeight * 1.1 : baseWeight,
            _ => baseWeight
        };
    }

    /// <summary>
    /// Returns user-type specific active hours for time shifting
    /// </summary>
    private int[] GetActiveHoursForUserType(UserType userType, bool isWeekend)
    {
        return userType switch
        {
            UserType.Student => isWeekend ? new[] { 11, 14, 16, 20, 22 } : new[] { 9, 12, 15, 18, 21 },
            UserType.YoungProfessional => isWeekend ? new[] { 10, 13, 16, 19, 21 } : new[] { 8, 12, 18, 20 },
            UserType.FamilyPerson => isWeekend ? new[] { 9, 11, 14, 17 } : new[] { 9, 12, 15, 17, 19 },
            UserType.Retiree => new[] { 10, 12, 14, 16 },
            UserType.SmallBusiness => new[] { 7, 10, 12, 15, 18 },
            UserType.Freelancer => new[] { 10, 14, 16, 19, 21 },
            UserType.HighNetWorth => new[] { 10, 12, 15, 18 },
            _ => new[] { 9, 12, 15, 18, 20 }
        };
    }

    /// <summary>
    /// Selects merchant category and spending pattern based on user profile and time
    /// </summary>
    private (MerchantCategory category, SpendingPattern pattern) SelectCategoryAndPattern(UserProfile profile, DateTimeOffset time)
    {
        var availablePatterns = profile.SpendingPatterns;

        if (!availablePatterns.Any())
        {
            var defaultCategory = GetDefaultCategoryForTime(time);
            var defaultPattern = new SpendingPattern(defaultCategory, 50.0, 20.0, 10);
            return (defaultCategory, defaultPattern);
        }

        var weightedPatterns = availablePatterns.Select(p => new
        {
            Pattern = p,
            Weight = CalculatePatternWeight(p, time)
        }).Where(w => w.Weight > 0).ToList();

        if (!weightedPatterns.Any())
        {
            var fallbackPattern = availablePatterns.First();
            return (fallbackPattern.Category, fallbackPattern);
        }

        var totalWeight = weightedPatterns.Sum(w => w.Weight);
        var randomValue = _random.NextDouble() * totalWeight;

        double currentWeight = 0;
        foreach (var weighted in weightedPatterns)
        {
            currentWeight += weighted.Weight;
            if (randomValue <= currentWeight)
            {
                return (weighted.Pattern.Category, weighted.Pattern);
            }
        }

        var selectedPattern = weightedPatterns.Last().Pattern;
        return (selectedPattern.Category, selectedPattern);
    }

    /// <summary>
    /// Calculates pattern weight based on frequency, time preference, and weekend factors
    /// </summary>
    private double CalculatePatternWeight(SpendingPattern pattern, DateTimeOffset time)
    {
        var baseWeight = pattern.MonthlyFrequency / (double)DAYS_IN_MONTH;
        var hourDiff = Math.Abs(time.Hour - pattern.PreferredHour);
        var timeWeight = Math.Max(MINIMUM_WEIGHT, 1.0 - (hourDiff / (double)HOURS_IN_HALF_DAY));
        var isWeekend = time.DayOfWeek == DayOfWeek.Saturday || time.DayOfWeek == DayOfWeek.Sunday;
        var weekendWeight = isWeekend ? pattern.WeekendProbability : (1.0 - pattern.WeekendProbability);

        return baseWeight * timeWeight * weekendWeight;
    }

    /// <summary>
    /// Returns default merchant category based on time of day
    /// </summary>
    private MerchantCategory GetDefaultCategoryForTime(DateTimeOffset time)
    {
        var hour = time.Hour;
        if (hour >= 6 && hour <= 9) return MerchantCategory.Restaurant;
        if (hour >= 11 && hour <= 14) return MerchantCategory.Restaurant;
        if (hour >= 17 && hour <= 19) return MerchantCategory.Grocery;
        return MerchantCategory.Retail;
    }

    /// <summary>
    /// Generates realistic transaction amount using normal distribution within category bounds
    /// </summary>
    private double GenerateRealisticAmount(SpendingPattern pattern, MerchantCategory category)
    {
        var gaussian = GenerateGaussianRandom();
        var amount = pattern.AverageAmount + (gaussian * pattern.AmountStdDev);

        if (_categoryAmountRanges.TryGetValue(category, out var range))
        {
            amount = Math.Max(range.min, Math.Min(range.max, amount));
        }

        return Math.Max(MINIMUM_AMOUNT, amount);
    }

    /// <summary>
    /// Generates normally distributed random number using Box-Muller transformation
    /// </summary>
    private double GenerateGaussianRandom()
    {
        double u1 = 1.0 - _random.NextDouble();
        double u2 = 1.0 - _random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }

    /// <summary>
    /// Determines transaction location based on user travel probability
    /// </summary>
    private Location GetTransactionLocation(UserProfile profile, DateTimeOffset time)
    {
        if (_random.NextDouble() > profile.TravelProbability)
        {
            return LocationData.GetNearbyLocation(profile.HomeLocation, _random);
        }
        return LocationData.GetRandomLocation(_random);
    }

    /// <summary>
    /// Maps merchant category to appropriate transaction type
    /// </summary>
    private TransactionType GetTransactionTypeForCategory(MerchantCategory category)
    {
        return category switch
        {
            MerchantCategory.ATM => TransactionType.ATMWithdrawal,
            MerchantCategory.Utilities => TransactionType.BillPayment,
            MerchantCategory.Subscription => TransactionType.Subscription,
            _ => TransactionType.Purchase
        };
    }

    /// <summary>
    /// Selects realistic payment method based on category and amount
    /// </summary>
    private PaymentMethod GetRealisticPaymentMethod(MerchantCategory category, double amount)
    {
        if (amount > LARGE_AMOUNT_THRESHOLD)
        {
            return _random.NextDouble() < CHECK_PROBABILITY ? PaymentMethod.Check : PaymentMethod.ACH;
        }

        if (category == MerchantCategory.ATM)
        {
            return PaymentMethod.Cash;
        }

        if (category == MerchantCategory.OnlineServices || category == MerchantCategory.Subscription)
        {
            return _random.NextDouble() < DIGITAL_WALLET_PROBABILITY ? PaymentMethod.DigitalWallet : PaymentMethod.CreditCard;
        }

        return _random.NextDouble() < DEBIT_CARD_PROBABILITY ? PaymentMethod.DebitCard : PaymentMethod.CreditCard;
    }

    /// <summary>
    /// Generates merchant account ID with category-specific prefix
    /// </summary>
    private Account GenerateDestinationAccount(MerchantCategory category, string merchantName)
    {
        var merchantPrefix = category switch
        {
            MerchantCategory.Grocery => "GRC",
            MerchantCategory.Restaurant => "RST",
            MerchantCategory.Gas => "GAS",
            MerchantCategory.Retail => "RTL",
            MerchantCategory.Healthcare => "HLT",
            MerchantCategory.Entertainment => "ENT",
            MerchantCategory.Travel => "TRV",
            MerchantCategory.Utilities => "UTL",
            MerchantCategory.OnlineServices => "ONL",
            MerchantCategory.ATM => "ATM",
            _ => "MER"
        };

        var merchantId = $"{merchantPrefix}{_random.Next(MERCHANT_ID_MIN, MERCHANT_ID_MAX)}";
        return new Account(merchantId);
    }

    /// <summary>
    /// Generates diverse user profiles with realistic characteristics
    /// </summary>
    private List<UserProfile> GenerateUserProfiles()
    {
        var profiles = new List<UserProfile>();
        var userTypes = Enum.GetValues<UserType>();
        
        for (int i = 0; i < USER_PROFILE_COUNT; i++)
        {
            var accountId = $"ACC{ACCOUNT_ID_BASE + i}";
            var userType = userTypes[_random.Next(userTypes.Length)];
            var homeLocation = LocationData.GetRandomLocation(_random);
            var timeZoneOffset = GetTimeZoneForState(homeLocation.State);

            var profile = new UserProfile(
                accountId: accountId,
                type: userType,
                monthlyIncome: GenerateIncomeForUserType(userType),
                homeLocation: homeLocation,
                timeZoneOffset: timeZoneOffset,
                spendingPatterns: GenerateSpendingPatternsForUserType(userType),
                riskTolerance: GenerateRiskToleranceForUserType(userType),
                travelProbability: GenerateTravelProbabilityForUserType(userType)
            );

            profiles.Add(profile);
        }

        return profiles;
    }

    /// <summary>
    /// Generates realistic monthly income based on user type
    /// </summary>
    private decimal GenerateIncomeForUserType(UserType userType)
    {
        return userType switch
        {
            UserType.Student => (decimal)(_random.NextDouble() * 2000 + 500),
            UserType.YoungProfessional => (decimal)(_random.NextDouble() * 4000 + 3000),
            UserType.FamilyPerson => (decimal)(_random.NextDouble() * 6000 + 4000),
            UserType.MiddleAged => (decimal)(_random.NextDouble() * 8000 + 5000),
            UserType.Retiree => (decimal)(_random.NextDouble() * 3000 + 2000),
            UserType.HighNetWorth => (decimal)(_random.NextDouble() * 20000 + 10000),
            UserType.SmallBusiness => (decimal)(_random.NextDouble() * 15000 + 5000),
            UserType.Freelancer => (decimal)(_random.NextDouble() * 5000 + 2000),
            _ => 3000
        };
    }

    /// <summary>
    /// Creates user-type specific spending patterns with base patterns for all users
    /// </summary>
    private List<SpendingPattern> GenerateSpendingPatternsForUserType(UserType userType)
    {
        var patterns = new List<SpendingPattern>
        {
            new(MerchantCategory.Grocery, 75, 25, 8, 18, 0.4),
            new(MerchantCategory.Restaurant, 35, 15, 6, 12, 0.6),
            new(MerchantCategory.Gas, 60, 20, 4, 16, 0.3)
        };

        switch (userType)
        {
            case UserType.Student:
                patterns.Add(new SpendingPattern(MerchantCategory.Education, 500, 200, 2, 14, 0.1));
                patterns.Add(new SpendingPattern(MerchantCategory.Entertainment, 25, 10, 8, 20, 0.8));
                break;
            case UserType.YoungProfessional:
                patterns.Add(new SpendingPattern(MerchantCategory.Entertainment, 50, 20, 12, 19, 0.7));
                patterns.Add(new SpendingPattern(MerchantCategory.Travel, 800, 300, 1, 15, 0.2));
                break;
            case UserType.FamilyPerson:
                patterns.Add(new SpendingPattern(MerchantCategory.Healthcare, 200, 100, 3, 11, 0.2));
                patterns.Add(new SpendingPattern(MerchantCategory.Education, 300, 150, 2, 15, 0.1));
                break;
            case UserType.HighNetWorth:
                patterns.Add(new SpendingPattern(MerchantCategory.Travel, 2000, 800, 3, 14, 0.3));
                patterns.Add(new SpendingPattern(MerchantCategory.Retail, 500, 200, 10, 15, 0.4));
                break;
        }

        return patterns;
    }

    /// <summary>
    /// Returns risk tolerance probability for generating anomalous transactions
    /// </summary>
    private double GenerateRiskToleranceForUserType(UserType userType)
    {
        return userType switch
        {
            UserType.Student => 0.15,
            UserType.YoungProfessional => 0.12,
            UserType.FamilyPerson => 0.08,
            UserType.MiddleAged => 0.10,
            UserType.Retiree => 0.05,
            UserType.HighNetWorth => 0.20,
            UserType.SmallBusiness => 0.25,
            UserType.Freelancer => 0.18,
            _ => 0.10
        };
    }

    /// <summary>
    /// Returns travel probability for generating transactions away from home location
    /// </summary>
    private double GenerateTravelProbabilityForUserType(UserType userType)
    {
        return userType switch
        {
            UserType.Student => 0.03,
            UserType.YoungProfessional => 0.08,
            UserType.FamilyPerson => 0.05,
            UserType.MiddleAged => 0.07,
            UserType.Retiree => 0.12,
            UserType.HighNetWorth => 0.15,
            UserType.SmallBusiness => 0.10,
            UserType.Freelancer => 0.06,
            _ => 0.05
        };
    }

    /// <summary>
    /// Maps US state codes to timezone offsets from UTC
    /// </summary>
    private int GetTimeZoneForState(string state)
    {
        return state switch
        {
            "CA" or "WA" or "OR" or "NV" => -8,
            "AZ" or "CO" or "NM" or "UT" => -7,
            "TX" or "IL" or "MO" or "WI" => -6,
            _ => -5
        };
    }

    /// <summary>
    /// Defines realistic amount ranges for each merchant category
    /// </summary>
    private Dictionary<MerchantCategory, (double min, double max)> InitializeCategoryAmountRanges()
    {
        return new Dictionary<MerchantCategory, (double, double)>
        {
            [MerchantCategory.Grocery] = (5.0, 300.0),
            [MerchantCategory.Restaurant] = (8.0, 150.0),
            [MerchantCategory.Gas] = (20.0, 120.0),
            [MerchantCategory.Retail] = (10.0, 2000.0),
            [MerchantCategory.Healthcare] = (25.0, 5000.0),
            [MerchantCategory.Entertainment] = (5.0, 500.0),
            [MerchantCategory.Travel] = (100.0, 5000.0),
            [MerchantCategory.Education] = (50.0, 2000.0),
            [MerchantCategory.Utilities] = (30.0, 400.0),
            [MerchantCategory.Insurance] = (100.0, 1000.0),
            [MerchantCategory.ATM] = (20.0, 500.0),
            [MerchantCategory.OnlineServices] = (5.0, 200.0)
        };
    }
}
