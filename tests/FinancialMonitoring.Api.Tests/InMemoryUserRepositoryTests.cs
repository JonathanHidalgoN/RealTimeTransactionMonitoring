using FinancialMonitoring.Abstractions;
using FinancialMonitoring.Api.Services;
using FinancialMonitoring.Models;
using Moq;

namespace FinancialMonitoring.Api.Tests;

public class InMemoryUserRepositoryTests
{
    private readonly InMemoryUserRepository _repository;
    private readonly Mock<IPasswordHashingService> _mockPasswordService;

    public InMemoryUserRepositoryTests()
    {
        _mockPasswordService = new Mock<IPasswordHashingService>();

        _mockPasswordService.Setup(x => x.GenerateRandomSalt())
            .Returns("test-salt-base64");
        _mockPasswordService.Setup(x => x.HashPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("hashed-password");

        _repository = new InMemoryUserRepository(_mockPasswordService.Object);
    }

    [Fact]
    public async Task GetByUsernameAsync_WithExistingUser_ReturnsUser()
    {
        var username = "admin";

        var result = await _repository.GetByUsernameAsync(username);

        Assert.NotNull(result);
        Assert.Equal(username, result.Username);
        Assert.Equal(AuthUserRole.Admin, result.Role);
    }

    [Fact]
    public async Task GetByUsernameAsync_WithNonExistingUser_ReturnsNull()
    {
        var username = "nonexistent";

        var result = await _repository.GetByUsernameAsync(username);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetByUsernameAsync_WithInvalidUsername_ReturnsNull(string? invalidUsername)
    {
        var result = await _repository.GetByUsernameAsync(invalidUsername!);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByEmailAsync_WithExistingEmail_ReturnsUser()
    {
        var email = "admin@financialmonitoring.com";

        var result = await _repository.GetByEmailAsync(email);

        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
        Assert.Equal("admin", result.Username);
    }

    [Fact]
    public async Task GetByEmailAsync_WithNonExistingEmail_ReturnsNull()
    {
        var email = "nonexistent@example.com";

        var result = await _repository.GetByEmailAsync(email);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsUser()
    {
        var userId = 1;

        var result = await _repository.GetByIdAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("admin", result.Username);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNull()
    {
        var userId = 999;

        var result = await _repository.GetByIdAsync(userId);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WithNewUser_CreatesAndReturnsUser()
    {
        var newUser = new AuthUser
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Role = AuthUserRole.Viewer,
            FirstName = "New",
            LastName = "User",
            PasswordHash = "dummy-hash",
            Salt = "dummy-salt",
            IsActive = true
        };

        var result = await _repository.CreateAsync(newUser);

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(newUser.Username, result.Username);
        Assert.Equal(newUser.Email, result.Email);
        Assert.Equal(newUser.Role, result.Role);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);

        var retrievedUser = await _repository.GetByUsernameAsync(newUser.Username);
        Assert.NotNull(retrievedUser);
        Assert.Equal(result.Id, retrievedUser.Id);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateUsername_AllowsCreation()
    {
        var existingUsername = "admin";
        var newUser = new AuthUser
        {
            Username = existingUsername,
            Email = "different@example.com",
            Role = AuthUserRole.Viewer,
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "dummy-hash",
            Salt = "dummy-salt",
            IsActive = true
        };

        var result = await _repository.CreateAsync(newUser);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_AllowsCreation()
    {
        var existingEmail = "admin@financialmonitoring.com";
        var newUser = new AuthUser
        {
            Username = "differentusername",
            Email = existingEmail,
            Role = AuthUserRole.Viewer,
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "dummy-hash",
            Salt = "dummy-salt",
            IsActive = true
        };

        var result = await _repository.CreateAsync(newUser);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task UpdateLastLoginAsync_WithExistingUser_UpdatesLastLogin()
    {
        var userId = 1;
        var userBefore = await _repository.GetByIdAsync(userId);
        var originalLastLogin = userBefore?.LastLoginAt;

        await _repository.UpdateLastLoginAsync(userId);

        var userAfter = await _repository.GetByIdAsync(userId);
        Assert.NotNull(userAfter);
        Assert.NotNull(userAfter.LastLoginAt);
        Assert.True(userAfter.LastLoginAt > originalLastLogin || (originalLastLogin == null && userAfter.LastLoginAt != null));
        Assert.True(userAfter.LastLoginAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task UpdateLastLoginAsync_WithNonExistingUser_DoesNotThrow()
    {
        var nonExistingUserId = 999;

        await _repository.UpdateLastLoginAsync(nonExistingUserId);
    }

    [Fact]
    public async Task Repository_HasCorrectNumberOfSeededUsers()
    {
        var admin = await _repository.GetByUsernameAsync("admin");
        var analyst = await _repository.GetByUsernameAsync("analyst");  
        var viewer = await _repository.GetByUsernameAsync("viewer");

        Assert.NotNull(admin);
        Assert.NotNull(analyst);
        Assert.NotNull(viewer);
    }

    [Fact]
    public async Task Repository_ContainsCorrectSeededData()
    {
        var admin = await _repository.GetByUsernameAsync("admin");
        var analyst = await _repository.GetByUsernameAsync("analyst");
        var viewer = await _repository.GetByUsernameAsync("viewer");

        Assert.NotNull(admin);
        Assert.Equal(AuthUserRole.Admin, admin.Role);
        Assert.Equal("admin@financialmonitoring.com", admin.Email);
        Assert.Equal("System", admin.FirstName);
        Assert.Equal("Administrator", admin.LastName);
        Assert.True(admin.IsActive);

        Assert.NotNull(analyst);
        Assert.Equal(AuthUserRole.Analyst, analyst.Role);
        Assert.Equal("analyst@financialmonitoring.com", analyst.Email);
        Assert.Equal("Financial", analyst.FirstName);
        Assert.Equal("Analyst", analyst.LastName);
        Assert.True(analyst.IsActive);

        Assert.NotNull(viewer);
        Assert.Equal(AuthUserRole.Viewer, viewer.Role);
        Assert.Equal("viewer@financialmonitoring.com", viewer.Email);
        Assert.Equal("Report", viewer.FirstName);
        Assert.Equal("Viewer", viewer.LastName);
        Assert.True(viewer.IsActive);
    }

    [Fact]
    public async Task CreateAsync_AssignsSequentialIds()
    {
        var user1 = new AuthUser
        {
            Username = "testuser1",
            Email = "test1@example.com",
            Role = AuthUserRole.Viewer,
            FirstName = "Test",
            LastName = "User1",
            PasswordHash = "hash1",
            Salt = "salt1",
            IsActive = true
        };

        var user2 = new AuthUser
        {
            Username = "testuser2",
            Email = "test2@example.com",
            Role = AuthUserRole.Viewer,
            FirstName = "Test",
            LastName = "User2",
            PasswordHash = "hash2",
            Salt = "salt2",
            IsActive = true
        };

        var createdUser1 = await _repository.CreateAsync(user1);
        var createdUser2 = await _repository.CreateAsync(user2);

        Assert.True(createdUser1.Id > 0);
        Assert.True(createdUser2.Id > createdUser1.Id);
        Assert.Equal(createdUser1.Id + 1, createdUser2.Id);
    }
}