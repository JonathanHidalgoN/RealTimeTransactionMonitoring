using FinancialMonitoring.Abstractions;
using FinancialMonitoring.Api.Controllers.V2;
using FinancialMonitoring.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinancialMonitoring.Api.Tests.V2;

public class AuthControllerV2Tests
{
    private readonly AuthController _controller;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IJwtTokenService> _mockJwtService;
    private readonly Mock<IPasswordHashingService> _mockPasswordService;

    public AuthControllerV2Tests()
    {
        _mockLogger = new Mock<ILogger<AuthController>>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockJwtService = new Mock<IJwtTokenService>();
        _mockPasswordService = new Mock<IPasswordHashingService>();

        _controller = new AuthController(
            _mockLogger.Object,
            _mockUserRepository.Object,
            _mockJwtService.Object,
            _mockPasswordService.Object);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccessWithTokens()
    {
        var testUser = new AuthUser
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            Role = AuthUserRole.Admin,
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hashed-password",
            Salt = "test-salt",
            IsActive = true
        };

        var loginRequest = new LoginRequest
        {
            Username = "testuser",
            Password = "password123"
        };

        _mockUserRepository.Setup(x => x.GetByUsernameAsync("testuser"))
            .ReturnsAsync(testUser);
        _mockPasswordService.Setup(x => x.VerifyPassword("password123", "hashed-password", "test-salt"))
            .Returns(true);
        _mockJwtService.Setup(x => x.GenerateAccessToken(testUser))
            .Returns("access-token");
        _mockJwtService.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");
        _mockJwtService.Setup(x => x.GetAccessTokenExpiration())
            .Returns(DateTime.UtcNow.AddMinutes(15));

        var result = await _controller.Login(loginRequest);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<LoginResponse>>(okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal("access-token", apiResponse.Data.AccessToken);
        Assert.Equal("refresh-token", apiResponse.Data.RefreshToken);
        Assert.NotNull(apiResponse.Data.User);
        Assert.Equal("testuser", apiResponse.Data.User.Username);

        _mockUserRepository.Verify(x => x.UpdateLastLoginAsync(1), Times.Once);
    }

    [Fact]
    public async Task Login_WithInvalidUsername_ReturnsUnauthorized()
    {
        var loginRequest = new LoginRequest
        {
            Username = "nonexistent",
            Password = "password123"
        };

        _mockUserRepository.Setup(x => x.GetByUsernameAsync("nonexistent"))
            .ReturnsAsync((AuthUser?)null);

        var result = await _controller.Login(loginRequest);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ApiErrorResponse>(unauthorizedResult.Value);

        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var testUser = new AuthUser
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            Role = AuthUserRole.Admin,
            PasswordHash = "hashed-password",
            Salt = "test-salt",
            IsActive = true
        };

        var loginRequest = new LoginRequest
        {
            Username = "testuser",
            Password = "wrongpassword"
        };

        _mockUserRepository.Setup(x => x.GetByUsernameAsync("testuser"))
            .ReturnsAsync(testUser);
        _mockPasswordService.Setup(x => x.VerifyPassword("wrongpassword", "hashed-password", "test-salt"))
            .Returns(false);

        var result = await _controller.Login(loginRequest);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ApiErrorResponse>(unauthorizedResult.Value);

        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);
    }

    [Fact]
    public async Task Login_WithInactiveUser_ReturnsUnauthorized()
    {
        var inactiveUser = new AuthUser
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            Role = AuthUserRole.Admin,
            PasswordHash = "hashed-password",
            Salt = "test-salt",
            IsActive = false
        };

        var loginRequest = new LoginRequest
        {
            Username = "testuser",
            Password = "password123"
        };

        _mockUserRepository.Setup(x => x.GetByUsernameAsync("testuser"))
            .ReturnsAsync(inactiveUser);
        _mockPasswordService.Setup(x => x.VerifyPassword("password123", "hashed-password", "test-salt"))
            .Returns(true);

        var result = await _controller.Login(loginRequest);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ApiErrorResponse>(unauthorizedResult.Value);

        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);
    }

    [Fact]
    public async Task Login_WithRepositoryException_ReturnsInternalServerError()
    {
        var loginRequest = new LoginRequest
        {
            Username = "testuser",
            Password = "password123"
        };

        _mockUserRepository.Setup(x => x.GetByUsernameAsync("testuser"))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var result = await _controller.Login(loginRequest);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);

        var errorResponse = Assert.IsType<ApiErrorResponse>(statusResult.Value);
        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);
    }
}