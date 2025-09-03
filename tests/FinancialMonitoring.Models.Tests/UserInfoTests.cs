namespace FinancialMonitoring.Models.Tests;

public class UserInfoTests
{
    [Fact]
    public void FromAuthUser_WithValidAuthUser_ReturnsCorrectUserInfo()
    {
        // Arrange
        var authUser = new AuthUser
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            Role = AuthUserRole.Admin,
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            CreatedAt = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            LastLoginAt = new DateTime(2023, 1, 2, 12, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var userInfo = UserInfo.FromAuthUser(authUser);

        // Assert
        Assert.NotNull(userInfo);
        Assert.Equal(authUser.Id, userInfo.Id);
        Assert.Equal(authUser.Username, userInfo.Username);
        Assert.Equal(authUser.Email, userInfo.Email);
        Assert.Equal(authUser.Role, userInfo.Role);
        Assert.Equal(authUser.FirstName, userInfo.FirstName);
        Assert.Equal(authUser.LastName, userInfo.LastName);
        Assert.Equal(authUser.IsActive, userInfo.IsActive);
        Assert.Equal(authUser.CreatedAt, userInfo.CreatedAt);
        Assert.Equal(authUser.LastLoginAt, userInfo.LastLoginAt);
    }

    [Fact]
    public void FromAuthUser_WithNullLastLogin_HandlesCorrectly()
    {
        var authUser = new AuthUser
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            Role = AuthUserRole.Viewer,
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = null
        };

        var userInfo = UserInfo.FromAuthUser(authUser);

        Assert.NotNull(userInfo);
        Assert.Null(userInfo.LastLoginAt);
    }

    [Theory]
    [InlineData(AuthUserRole.Admin)]
    [InlineData(AuthUserRole.Analyst)]
    [InlineData(AuthUserRole.Viewer)]
    public void FromAuthUser_WithDifferentRoles_PreservesRole(AuthUserRole authRole)
    {
        var authUser = new AuthUser
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            Role = authRole,
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var userInfo = UserInfo.FromAuthUser(authUser);

        Assert.Equal(authRole, userInfo.Role);
    }

    [Fact]
    public void FromAuthUser_WithInactiveUser_PreservesIsActiveFlag()
    {
        var authUser = new AuthUser
        {
            Id = 1,
            Username = "inactiveuser",
            Email = "inactive@example.com",
            Role = AuthUserRole.Viewer,
            FirstName = "Inactive",
            LastName = "User",
            IsActive = false, // User is inactive
            CreatedAt = DateTime.UtcNow
        };

        var userInfo = UserInfo.FromAuthUser(authUser);

        Assert.False(userInfo.IsActive);
    }

    [Fact]
    public void FromAuthUser_PreservesAllTimestamps()
    {
        var createdAt = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var lastLogin = new DateTime(2023, 12, 25, 15, 30, 45, DateTimeKind.Utc);

        var authUser = new AuthUser
        {
            Id = 1,
            Username = "timestampuser",
            Email = "timestamp@example.com",
            Role = AuthUserRole.Analyst,
            FirstName = "Time",
            LastName = "Stamp",
            IsActive = true,
            CreatedAt = createdAt,
            LastLoginAt = lastLogin
        };

        var userInfo = UserInfo.FromAuthUser(authUser);

        Assert.Equal(createdAt, userInfo.CreatedAt);
        Assert.Equal(lastLogin, userInfo.LastLoginAt);
    }
}
