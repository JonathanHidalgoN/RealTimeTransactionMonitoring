using FinancialMonitoring.Api.Validation;
using FluentValidation.TestHelper;

namespace FinancialMonitoring.Api.Tests.Services;

/// <summary>
/// Tests for input validation functionality
/// </summary>
public class ValidationTests
{
    private readonly TransactionQueryValidator _queryValidator;

    public ValidationTests()
    {
        _queryValidator = new TransactionQueryValidator();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void TransactionQueryValidator_InvalidPageNumber_ShouldHaveValidationError(int pageNumber)
    {
        var request = new TransactionQueryRequest { PageNumber = pageNumber };

        var result = _queryValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.PageNumber);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(9999)]
    public void TransactionQueryValidator_ValidPageNumber_ShouldNotHaveValidationError(int pageNumber)
    {
        var request = new TransactionQueryRequest { PageNumber = pageNumber };

        var result = _queryValidator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.PageNumber);
    }

    [Theory]
    [InlineData(10001)]
    [InlineData(50000)]
    public void TransactionQueryValidator_PageNumberTooLarge_ShouldHaveValidationError(int pageNumber)
    {
        var request = new TransactionQueryRequest { PageNumber = pageNumber };

        var result = _queryValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
              .WithErrorMessage("Page number cannot exceed 10000 for performance reasons");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void TransactionQueryValidator_InvalidPageSize_ShouldHaveValidationError(int pageSize)
    {
        var request = new TransactionQueryRequest { PageSize = pageSize };

        var result = _queryValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(500)]
    [InlineData(1000)]
    public void TransactionQueryValidator_PageSizeTooLarge_ShouldHaveValidationError(int pageSize)
    {
        var request = new TransactionQueryRequest { PageSize = pageSize };

        var result = _queryValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.PageSize)
              .WithErrorMessage("Page size cannot exceed 100 items per page");
    }

    [Fact]
    public void TransactionQueryValidator_FutureDates_ShouldHaveValidationError()
    {
        var futureDate = DateTime.UtcNow.AddDays(1);
        var request = new TransactionQueryRequest
        {
            StartDate = futureDate,
            EndDate = futureDate.AddDays(1)
        };

        var result = _queryValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.StartDate)
              .WithErrorMessage("Start date cannot be in the future");
        result.ShouldHaveValidationErrorFor(x => x.EndDate)
              .WithErrorMessage("End date cannot be in the future");
    }

    [Fact]
    public void TransactionQueryValidator_EndDateBeforeStartDate_ShouldHaveValidationError()
    {
        var endDate = DateTime.UtcNow.AddDays(-2);
        var startDate = DateTime.UtcNow.AddDays(-1);
        var request = new TransactionQueryRequest
        {
            StartDate = startDate,
            EndDate = endDate
        };

        var result = _queryValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.EndDate)
              .WithErrorMessage("End date must be after start date");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void TransactionQueryValidator_NegativeMinAmount_ShouldHaveValidationError(decimal minAmount)
    {
        var request = new TransactionQueryRequest { MinAmount = minAmount };

        var result = _queryValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.MinAmount)
              .WithErrorMessage("Minimum amount cannot be negative");
    }

    [Fact]
    public void TransactionQueryValidator_MaxAmountTooLarge_ShouldHaveValidationError()
    {
        var request = new TransactionQueryRequest
        {
            MaxAmount = 2_000_000_000
        };

        var result = _queryValidator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.MaxAmount)
              .WithErrorMessage("Maximum amount cannot exceed 1 billion for security reasons");
    }
}
