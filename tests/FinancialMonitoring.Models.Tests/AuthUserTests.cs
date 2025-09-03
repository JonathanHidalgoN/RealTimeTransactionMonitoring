using System.ComponentModel.DataAnnotations;

namespace FinancialMonitoring.Models.Tests;

public class AuthUserTests
{

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AuthUser_WithInvalidUsername_FailsValidation(string username)
    {
        var user = new AuthUser
        {
            Username = username,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hashed-password",
            Salt = "salt-value"
        };

        var validationContext = new ValidationContext(user);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(user, validationContext, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, vr => vr.ErrorMessage != null);
    }

    [Fact]
    public void AuthUser_WithTooLongUsername_FailsValidation()
    {
        var user = new AuthUser
        {
            Username = new string('a', 101), // Max length is 100
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hashed-password",
            Salt = "salt-value"
        };

        var validationContext = new ValidationContext(user);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(user, validationContext, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, vr => vr.ErrorMessage != null);
    }

    [Fact]
    public void AuthUser_WithNullFirstName_PassesValidation()
    {
        var user = new AuthUser
        {
            Username = "testuser",
            Email = "test@example.com",
            FirstName = null,
            LastName = "User",
            PasswordHash = "hashed-password",
            Salt = "salt-value"
        };

        var validationContext = new ValidationContext(user);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(user, validationContext, validationResults, true);

        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AuthUser_WithInvalidPasswordHash_FailsValidation(string passwordHash)
    {
        var user = new AuthUser
        {
            Username = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = passwordHash,
            Salt = "salt-value"
        };

        var validationContext = new ValidationContext(user);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(user, validationContext, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, vr => vr.ErrorMessage != null);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AuthUser_WithInvalidSalt_FailsValidation(string salt)
    {
        var user = new AuthUser
        {
            Username = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hashed-password",
            Salt = salt
        };

        var validationContext = new ValidationContext(user);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(user, validationContext, validationResults, true);

        Assert.False(isValid);
        Assert.Contains(validationResults, vr => vr.ErrorMessage != null);
    }

    [Fact]
    public void AuthUser_CreatedAt_IsSetOnConstruction()
    {
        var beforeCreation = DateTime.UtcNow;

        var user = new AuthUser();

        var afterCreation = DateTime.UtcNow;

        Assert.True(user.CreatedAt >= beforeCreation);
        Assert.True(user.CreatedAt <= afterCreation);
    }

    [Theory]
    [InlineData(AuthUserRole.Admin)]
    [InlineData(AuthUserRole.Analyst)]
    [InlineData(AuthUserRole.Viewer)]
    public void AuthUser_WithDifferentRoles_AreValid(AuthUserRole role)
    {
        var user = new AuthUser
        {
            Username = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hashed-password",
            Salt = "salt-value",
            Role = role
        };

        var validationContext = new ValidationContext(user);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(user, validationContext, validationResults, true);

        Assert.True(isValid);
        Assert.Equal(role, user.Role);
    }
}
