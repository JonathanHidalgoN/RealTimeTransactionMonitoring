using FluentValidation;

namespace FinancialMonitoring.Api.Validation;

/// <summary>
/// Validator for transaction query parameters
/// </summary>
public class TransactionQueryValidator : AbstractValidator<TransactionQueryRequest>
{
    public TransactionQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0")
            .LessThanOrEqualTo(10000)
            .WithMessage("Page number cannot exceed 10000 for performance reasons");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100)
            .WithMessage("Page size cannot exceed 100 items per page");

        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Start date cannot be in the future")
            .When(x => x.StartDate.HasValue);

        RuleFor(x => x.EndDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("End date cannot be in the future")
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be after start date")
            .When(x => x.EndDate.HasValue);

        RuleFor(x => x.MinAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Minimum amount cannot be negative")
            .When(x => x.MinAmount.HasValue);

        RuleFor(x => x.MaxAmount)
            .GreaterThanOrEqualTo(x => x.MinAmount)
            .WithMessage("Maximum amount must be greater than or equal to minimum amount")
            .LessThanOrEqualTo(1_000_000_000)
            .WithMessage("Maximum amount cannot exceed 1 billion for security reasons")
            .When(x => x.MaxAmount.HasValue);
    }
}

/// <summary>
/// Request model for transaction queries with validation support
/// </summary>
public class TransactionQueryRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string? MerchantCategory { get; set; }
    public string? TransactionType { get; set; }
}
