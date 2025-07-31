using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace FinancialMonitoring.Api.Validation;

/// <summary>
/// Custom validation attribute for transaction IDs
/// </summary>
public class ValidTransactionIdAttribute : ValidationAttribute
{
    private static readonly Regex ValidIdPattern = new(@"^[a-zA-Z0-9\-_]{1,50}$", RegexOptions.Compiled);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string id)
        {
            return new ValidationResult("Transaction ID must be a string");
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            return new ValidationResult("Transaction ID is required");
        }

        if (!ValidIdPattern.IsMatch(id))
        {
            return new ValidationResult("Transaction ID can only contain letters, numbers, hyphens, and underscores");
        }

        return ValidationResult.Success;
    }
}