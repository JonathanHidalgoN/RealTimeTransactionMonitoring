using FinancialMonitoring.Abstractions;
using FinancialMonitoring.Api.Controllers.V2;
using FinancialMonitoring.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace FinancialMonitoring.Api.Tests.V2;

public static class TestConstants
{
    public const string ValidUsername = "testuser";
    public const string ValidEmail = "test@example.com";
    public const string ValidPassword = "password123";
    public const string HashedPassword = "hashed-password";
    public const string TestSalt = "test-salt";
    public const string AccessToken = "access-token";
    public const string RefreshToken = "refresh-token";
    public const int ValidUserId = 1;
    public const string InvalidUsername = "nonexistent";
    public const string WrongPassword = "wrongpassword";
    public const string InvalidRefreshToken = "invalid-refresh-token";
}

public static class AuthUserTestBuilder
{
    public static AuthUser CreateValidUser(int id = TestConstants.ValidUserId, bool isActive = true) => new()
    {
        Id = id,
        Username = TestConstants.ValidUsername,
        Email = TestConstants.ValidEmail,
        Role = AuthUserRole.Admin,
        FirstName = "Test",
        LastName = "User",
        PasswordHash = TestConstants.HashedPassword,
        Salt = TestConstants.TestSalt,
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow.AddDays(-30),
        LastLoginAt = DateTime.UtcNow.AddHours(-2)
    };

    public static AuthUser CreateInactiveUser(int id = TestConstants.ValidUserId) =>
        CreateValidUser(id, isActive: false);
}

public static class RequestTestBuilder
{
    public static LoginRequest CreateValidLoginRequest() => new()
    {
        Username = TestConstants.ValidUsername,
        Password = TestConstants.ValidPassword
    };

    public static LoginRequest CreateInvalidUsernameRequest() => new()
    {
        Username = TestConstants.InvalidUsername,
        Password = TestConstants.ValidPassword
    };

    public static LoginRequest CreateInvalidPasswordRequest() => new()
    {
        Username = TestConstants.ValidUsername,
        Password = TestConstants.WrongPassword
    };

    public static RefreshTokenRequest CreateValidRefreshTokenRequest() => new()
    {
        RefreshToken = TestConstants.RefreshToken
    };

    public static RefreshTokenRequest CreateInvalidRefreshTokenRequest() => new()
    {
        RefreshToken = TestConstants.InvalidRefreshToken
    };

    public static RegisterRequest CreateValidRegisterRequest() => new()
    {
        Username = "newuser",
        Email = "newuser@example.com",
        Password = "newpassword123",
        Role = AuthUserRole.Analyst,
        FirstName = "New",
        LastName = "User"
    };
}

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

    private void SetupAuthenticatedUser(int userId = TestConstants.ValidUserId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    private void SetupValidPasswordVerification()
    {
        _mockPasswordService.Setup(x => x.VerifyPassword(
            TestConstants.ValidPassword,
            TestConstants.HashedPassword,
            TestConstants.TestSalt))
            .Returns(true);
    }

    private void SetupJwtTokenGeneration()
    {
        _mockJwtService.Setup(x => x.GenerateAccessToken(It.IsAny<AuthUser>()))
            .Returns(TestConstants.AccessToken);
        _mockJwtService.Setup(x => x.GenerateRefreshToken())
            .Returns(TestConstants.RefreshToken);
        _mockJwtService.Setup(x => x.GetAccessTokenExpiration())
            .Returns(DateTime.UtcNow.AddMinutes(15));
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccessWithTokens()
    {
        var testUser = AuthUserTestBuilder.CreateValidUser();
        var loginRequest = RequestTestBuilder.CreateValidLoginRequest();

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(TestConstants.ValidUsername))
            .ReturnsAsync(testUser);
        SetupValidPasswordVerification();
        SetupJwtTokenGeneration();

        var result = await _controller.Login(loginRequest);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<LoginResponse>>(okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(TestConstants.AccessToken, apiResponse.Data.AccessToken);
        Assert.Equal(TestConstants.RefreshToken, apiResponse.Data.RefreshToken);
        Assert.NotNull(apiResponse.Data.User);
        Assert.Equal(TestConstants.ValidUsername, apiResponse.Data.User.Username);
        Assert.Equal(TestConstants.ValidEmail, apiResponse.Data.User.Email);
        Assert.Equal(AuthUserRole.Admin, apiResponse.Data.User.Role);

        _mockUserRepository.Verify(x => x.UpdateLastLoginAsync(TestConstants.ValidUserId), Times.Once);
        _mockUserRepository.Verify(x => x.GetByUsernameAsync(TestConstants.ValidUsername), Times.Once);
        _mockPasswordService.Verify(x => x.VerifyPassword(
            TestConstants.ValidPassword,
            TestConstants.HashedPassword,
            TestConstants.TestSalt), Times.Once);
    }

    [Fact]
    public async Task Login_WithInvalidUsername_ReturnsUnauthorized()
    {
        var loginRequest = RequestTestBuilder.CreateInvalidUsernameRequest();

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(TestConstants.InvalidUsername))
            .ReturnsAsync((AuthUser?)null);

        var result = await _controller.Login(loginRequest);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ApiErrorResponse>(unauthorizedResult.Value);

        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);

        _mockUserRepository.Verify(x => x.GetByUsernameAsync(TestConstants.InvalidUsername), Times.Once);
        _mockPasswordService.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var testUser = AuthUserTestBuilder.CreateValidUser();
        var loginRequest = RequestTestBuilder.CreateInvalidPasswordRequest();

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(TestConstants.ValidUsername))
            .ReturnsAsync(testUser);
        _mockPasswordService.Setup(x => x.VerifyPassword(
            TestConstants.WrongPassword,
            TestConstants.HashedPassword,
            TestConstants.TestSalt))
            .Returns(false);

        var result = await _controller.Login(loginRequest);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ApiErrorResponse>(unauthorizedResult.Value);

        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);

        _mockUserRepository.Verify(x => x.UpdateLastLoginAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithInactiveUser_ReturnsUnauthorized()
    {
        var inactiveUser = AuthUserTestBuilder.CreateInactiveUser();
        var loginRequest = RequestTestBuilder.CreateValidLoginRequest();

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(TestConstants.ValidUsername))
            .ReturnsAsync(inactiveUser);
        SetupValidPasswordVerification();

        var result = await _controller.Login(loginRequest);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ApiErrorResponse>(unauthorizedResult.Value);

        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);

        _mockUserRepository.Verify(x => x.UpdateLastLoginAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithRepositoryException_ReturnsInternalServerError()
    {
        var loginRequest = RequestTestBuilder.CreateValidLoginRequest();

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(TestConstants.ValidUsername))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var result = await _controller.Login(loginRequest);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);

        var errorResponse = Assert.IsType<ApiErrorResponse>(statusResult.Value);
        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);

        _mockPasswordService.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockUserRepository.Verify(x => x.UpdateLastLoginAsync(It.IsAny<int>()), Times.Never);
    }


    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewTokens()
    {
        var testUser = AuthUserTestBuilder.CreateValidUser();
        var refreshRequest = RequestTestBuilder.CreateValidRefreshTokenRequest();

        _mockJwtService.Setup(x => x.ValidateRefreshToken(TestConstants.RefreshToken))
            .Returns(TestConstants.ValidUserId);
        _mockUserRepository.Setup(x => x.GetByIdAsync(TestConstants.ValidUserId))
            .ReturnsAsync(testUser);
        SetupJwtTokenGeneration();

        var result = await _controller.RefreshToken(refreshRequest);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<RefreshTokenResponse>>(okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(TestConstants.AccessToken, apiResponse.Data.AccessToken);
        Assert.Equal(TestConstants.RefreshToken, apiResponse.Data.RefreshToken);
        Assert.True(apiResponse.Data.ExpiresAt > DateTime.UtcNow);

        _mockJwtService.Verify(x => x.ValidateRefreshToken(TestConstants.RefreshToken), Times.Once);
        _mockJwtService.Verify(x => x.InvalidateRefreshToken(TestConstants.RefreshToken), Times.Once);
        _mockUserRepository.Verify(x => x.GetByIdAsync(TestConstants.ValidUserId), Times.Once);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ReturnsUnauthorized()
    {
        var refreshRequest = RequestTestBuilder.CreateInvalidRefreshTokenRequest();

        _mockJwtService.Setup(x => x.ValidateRefreshToken(TestConstants.InvalidRefreshToken))
            .Returns((int?)null);

        var result = await _controller.RefreshToken(refreshRequest);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ApiErrorResponse>(unauthorizedResult.Value);

        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);

        _mockJwtService.Verify(x => x.ValidateRefreshToken(TestConstants.InvalidRefreshToken), Times.Once);
        _mockUserRepository.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _mockJwtService.Verify(x => x.InvalidateRefreshToken(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RefreshToken_WithUserNotFound_ReturnsUnauthorized()
    {
        var refreshRequest = RequestTestBuilder.CreateValidRefreshTokenRequest();

        _mockJwtService.Setup(x => x.ValidateRefreshToken(TestConstants.RefreshToken))
            .Returns(TestConstants.ValidUserId);
        _mockUserRepository.Setup(x => x.GetByIdAsync(TestConstants.ValidUserId))
            .ReturnsAsync((AuthUser?)null);

        var result = await _controller.RefreshToken(refreshRequest);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ApiErrorResponse>(unauthorizedResult.Value);

        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);

        _mockJwtService.Verify(x => x.InvalidateRefreshToken(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RefreshToken_WithInactiveUser_ReturnsUnauthorized()
    {
        var inactiveUser = AuthUserTestBuilder.CreateInactiveUser();
        var refreshRequest = RequestTestBuilder.CreateValidRefreshTokenRequest();

        _mockJwtService.Setup(x => x.ValidateRefreshToken(TestConstants.RefreshToken))
            .Returns(TestConstants.ValidUserId);
        _mockUserRepository.Setup(x => x.GetByIdAsync(TestConstants.ValidUserId))
            .ReturnsAsync(inactiveUser);

        var result = await _controller.RefreshToken(refreshRequest);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ApiErrorResponse>(unauthorizedResult.Value);

        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);

        _mockJwtService.Verify(x => x.InvalidateRefreshToken(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RefreshToken_WithJwtServiceException_ReturnsInternalServerError()
    {
        var refreshRequest = RequestTestBuilder.CreateValidRefreshTokenRequest();

        _mockJwtService.Setup(x => x.ValidateRefreshToken(TestConstants.RefreshToken))
            .Throws(new InvalidOperationException("JWT validation error"));

        var result = await _controller.RefreshToken(refreshRequest);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);

        var errorResponse = Assert.IsType<ApiErrorResponse>(statusResult.Value);
        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);

        _mockUserRepository.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }


    #region Logout Tests

    [Fact]
    public void Logout_WithValidRefreshToken_ReturnsSuccess()
    {
        SetupAuthenticatedUser();
        var logoutRequest = RequestTestBuilder.CreateValidRefreshTokenRequest();

        var result = _controller.Logout(logoutRequest);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);

        _mockJwtService.Verify(x => x.InvalidateRefreshToken(TestConstants.RefreshToken), Times.Once);
    }

    [Fact]
    public void Logout_WithJwtServiceException_ReturnsInternalServerError()
    {
        SetupAuthenticatedUser();
        var logoutRequest = RequestTestBuilder.CreateValidRefreshTokenRequest();

        _mockJwtService.Setup(x => x.InvalidateRefreshToken(TestConstants.RefreshToken))
            .Throws(new InvalidOperationException("Token invalidation error"));

        var result = _controller.Logout(logoutRequest);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);

        var errorResponse = Assert.IsType<ApiErrorResponse>(statusResult.Value);
        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);
    }

    #endregion

    #region Register Tests

    [Fact]
    public async Task Register_WithValidRequest_ReturnsCreatedUser()
    {
        SetupAuthenticatedUser();
        var registerRequest = RequestTestBuilder.CreateValidRegisterRequest();
        var createdUser = AuthUserTestBuilder.CreateValidUser(2);
        createdUser.Username = registerRequest.Username;
        createdUser.Email = registerRequest.Email;
        createdUser.Role = registerRequest.Role;

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(registerRequest.Username))
            .ReturnsAsync((AuthUser?)null);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(registerRequest.Email))
            .ReturnsAsync((AuthUser?)null);
        _mockPasswordService.Setup(x => x.GenerateRandomSalt())
            .Returns(TestConstants.TestSalt);
        _mockPasswordService.Setup(x => x.HashPassword(registerRequest.Password, TestConstants.TestSalt))
            .Returns(TestConstants.HashedPassword);
        _mockUserRepository.Setup(x => x.CreateAsync(It.IsAny<AuthUser>()))
            .ReturnsAsync(createdUser);

        var result = await _controller.Register(registerRequest);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<RegisterResponse>>(createdResult.Value);

        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(createdUser.Id, apiResponse.Data.Id);
        Assert.Equal(registerRequest.Username, apiResponse.Data.Username);
        Assert.Equal(registerRequest.Email, apiResponse.Data.Email);
        Assert.Equal(registerRequest.Role, apiResponse.Data.Role);

        _mockUserRepository.Verify(x => x.GetByUsernameAsync(registerRequest.Username), Times.Once);
        _mockUserRepository.Verify(x => x.GetByEmailAsync(registerRequest.Email), Times.Once);
        _mockPasswordService.Verify(x => x.GenerateRandomSalt(), Times.Once);
        _mockPasswordService.Verify(x => x.HashPassword(registerRequest.Password, TestConstants.TestSalt), Times.Once);
        _mockUserRepository.Verify(x => x.CreateAsync(It.Is<AuthUser>(u =>
            u.Username == registerRequest.Username &&
            u.Email == registerRequest.Email &&
            u.Role == registerRequest.Role &&
            u.IsActive == true)), Times.Once);
    }

    [Fact]
    public async Task Register_WithExistingUsername_ReturnsConflict()
    {
        SetupAuthenticatedUser();
        var registerRequest = RequestTestBuilder.CreateValidRegisterRequest();
        var existingUser = AuthUserTestBuilder.CreateValidUser();

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(registerRequest.Username))
            .ReturnsAsync(existingUser);

        var result = await _controller.Register(registerRequest);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ApiErrorResponse>(conflictResult.Value);

        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);

        _mockUserRepository.Verify(x => x.GetByEmailAsync(It.IsAny<string>()), Times.Never);
        _mockUserRepository.Verify(x => x.CreateAsync(It.IsAny<AuthUser>()), Times.Never);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsConflict()
    {
        SetupAuthenticatedUser();
        var registerRequest = RequestTestBuilder.CreateValidRegisterRequest();
        var existingUser = AuthUserTestBuilder.CreateValidUser();

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(registerRequest.Username))
            .ReturnsAsync((AuthUser?)null);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(registerRequest.Email))
            .ReturnsAsync(existingUser);

        var result = await _controller.Register(registerRequest);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ApiErrorResponse>(conflictResult.Value);

        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);

        _mockUserRepository.Verify(x => x.CreateAsync(It.IsAny<AuthUser>()), Times.Never);
    }

    [Fact]
    public async Task Register_WithRepositoryException_ReturnsInternalServerError()
    {
        SetupAuthenticatedUser();
        var registerRequest = RequestTestBuilder.CreateValidRegisterRequest();

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(registerRequest.Username))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var result = await _controller.Register(registerRequest);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);

        var errorResponse = Assert.IsType<ApiErrorResponse>(statusResult.Value);
        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);

        _mockUserRepository.Verify(x => x.CreateAsync(It.IsAny<AuthUser>()), Times.Never);
    }

    [Fact]
    public async Task GetMe_WithValidUser_ReturnsUserInfo()
    {
        var testUser = AuthUserTestBuilder.CreateValidUser();
        SetupAuthenticatedUser(TestConstants.ValidUserId);

        _mockUserRepository.Setup(x => x.GetByIdAsync(TestConstants.ValidUserId))
            .ReturnsAsync(testUser);

        var result = await _controller.GetMe();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<UserInfo>>(okResult.Value);

        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(testUser.Id, apiResponse.Data.Id);
        Assert.Equal(testUser.Username, apiResponse.Data.Username);
        Assert.Equal(testUser.Email, apiResponse.Data.Email);
        Assert.Equal(testUser.Role, apiResponse.Data.Role);
        Assert.Equal(testUser.FirstName, apiResponse.Data.FirstName);
        Assert.Equal(testUser.LastName, apiResponse.Data.LastName);

        _mockUserRepository.Verify(x => x.GetByIdAsync(TestConstants.ValidUserId), Times.Once);
    }

    [Fact]
    public async Task GetMe_WithInvalidUserIdInToken_ReturnsUnauthorized()
    {
        var controller = new AuthController(
            _mockLogger.Object,
            _mockUserRepository.Object,
            _mockJwtService.Object,
            _mockPasswordService.Object);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "invalid-user-id")
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        var result = await controller.GetMe();

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ApiErrorResponse>(unauthorizedResult.Value);

        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);

        _mockUserRepository.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetMe_WithMissingUserIdClaim_ReturnsUnauthorized()
    {
        var controller = new AuthController(
            _mockLogger.Object,
            _mockUserRepository.Object,
            _mockJwtService.Object,
            _mockPasswordService.Object);

        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };

        var result = await controller.GetMe();

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ApiErrorResponse>(unauthorizedResult.Value);

        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);

        _mockUserRepository.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetMe_WithUserNotFound_ReturnsNotFound()
    {
        SetupAuthenticatedUser(TestConstants.ValidUserId);

        _mockUserRepository.Setup(x => x.GetByIdAsync(TestConstants.ValidUserId))
            .ReturnsAsync((AuthUser?)null);

        var result = await _controller.GetMe();

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ApiErrorResponse>(notFoundResult.Value);

        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);

        _mockUserRepository.Verify(x => x.GetByIdAsync(TestConstants.ValidUserId), Times.Once);
    }

    [Fact]
    public async Task GetMe_WithRepositoryException_ReturnsInternalServerError()
    {
        SetupAuthenticatedUser(TestConstants.ValidUserId);

        _mockUserRepository.Setup(x => x.GetByIdAsync(TestConstants.ValidUserId))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var result = await _controller.GetMe();

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);

        var errorResponse = Assert.IsType<ApiErrorResponse>(statusResult.Value);
        Assert.False(errorResponse.Success);
        Assert.NotNull(errorResponse.Error);
    }

    #endregion

    #region Security and Edge Case Tests

    [Theory]
    [InlineData("'; DROP TABLE Users; --")]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("admin'/*")]
    public async Task Login_WithMaliciousUsername_ReturnsUnauthorizedSafely(string maliciousUsername)
    {
        var loginRequest = new LoginRequest
        {
            Username = maliciousUsername,
            Password = TestConstants.ValidPassword
        };

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(maliciousUsername))
            .ReturnsAsync((AuthUser?)null);

        var result = await _controller.Login(loginRequest);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var errorResponse = Assert.IsType<ApiErrorResponse>(unauthorizedResult.Value);

        Assert.False(errorResponse.Success);

        _mockPasswordService.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Login_WithNullOrEmptyUsername_ReturnsUnauthorized(string? invalidUsername)
    {
        var loginRequest = new LoginRequest
        {
            Username = invalidUsername ?? string.Empty,
            Password = TestConstants.ValidPassword
        };

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
            .ReturnsAsync((AuthUser?)null);

        var result = await _controller.Login(loginRequest);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.False(((ApiErrorResponse)unauthorizedResult.Value!).Success);
    }

    [Fact]
    public async Task Login_WithExtremelyLongUsername_HandlesGracefully()
    {
        var longUsername = new string('a', 10000);
        var loginRequest = new LoginRequest
        {
            Username = longUsername,
            Password = TestConstants.ValidPassword
        };

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(longUsername))
            .ReturnsAsync((AuthUser?)null);

        var result = await _controller.Login(loginRequest);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.False(((ApiErrorResponse)unauthorizedResult.Value!).Success);
    }

    [Fact]
    public async Task Login_WithPasswordHashingServiceException_ReturnsInternalServerError()
    {
        var testUser = AuthUserTestBuilder.CreateValidUser();
        var loginRequest = RequestTestBuilder.CreateValidLoginRequest();

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(TestConstants.ValidUsername))
            .ReturnsAsync(testUser);
        _mockPasswordService.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new InvalidOperationException("Password hashing error"));

        var result = await _controller.Login(loginRequest);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);

        var errorResponse = Assert.IsType<ApiErrorResponse>(statusResult.Value);
        Assert.False(errorResponse.Success);

        _mockUserRepository.Verify(x => x.UpdateLastLoginAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithJwtServiceException_ReturnsInternalServerError()
    {
        var testUser = AuthUserTestBuilder.CreateValidUser();
        var loginRequest = RequestTestBuilder.CreateValidLoginRequest();

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(TestConstants.ValidUsername))
            .ReturnsAsync(testUser);
        SetupValidPasswordVerification();
        _mockJwtService.Setup(x => x.GenerateAccessToken(It.IsAny<AuthUser>()))
            .Throws(new InvalidOperationException("JWT generation error"));

        var result = await _controller.Login(loginRequest);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);

        var errorResponse = Assert.IsType<ApiErrorResponse>(statusResult.Value);
        Assert.False(errorResponse.Success);

        _mockUserRepository.Verify(x => x.UpdateLastLoginAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithUpdateLastLoginException_StillReturnsSuccess()
    {
        var testUser = AuthUserTestBuilder.CreateValidUser();
        var loginRequest = RequestTestBuilder.CreateValidLoginRequest();

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(TestConstants.ValidUsername))
            .ReturnsAsync(testUser);
        SetupValidPasswordVerification();
        SetupJwtTokenGeneration();
        _mockUserRepository.Setup(x => x.UpdateLastLoginAsync(TestConstants.ValidUserId))
            .ThrowsAsync(new InvalidOperationException("Update last login error"));

        var result = await _controller.Login(loginRequest);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);

        var errorResponse = Assert.IsType<ApiErrorResponse>(statusResult.Value);
        Assert.False(errorResponse.Success);
    }

    [Theory]
    [InlineData("newuser@example.com")]
    [InlineData("NEW.USER@EXAMPLE.COM")]
    [InlineData("new+tag@example.com")]
    public async Task Register_WithVariousEmailFormats_HandlesCorrectly(string email)
    {
        SetupAuthenticatedUser();
        var registerRequest = RequestTestBuilder.CreateValidRegisterRequest();
        registerRequest.Email = email;
        var createdUser = AuthUserTestBuilder.CreateValidUser(2);

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(registerRequest.Username))
            .ReturnsAsync((AuthUser?)null);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(registerRequest.Email))
            .ReturnsAsync((AuthUser?)null);
        _mockPasswordService.Setup(x => x.GenerateRandomSalt())
            .Returns(TestConstants.TestSalt);
        _mockPasswordService.Setup(x => x.HashPassword(registerRequest.Password, TestConstants.TestSalt))
            .Returns(TestConstants.HashedPassword);
        _mockUserRepository.Setup(x => x.CreateAsync(It.IsAny<AuthUser>()))
            .ReturnsAsync(createdUser);

        var result = await _controller.Register(registerRequest);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var apiResponse = Assert.IsType<ApiResponse<RegisterResponse>>(createdResult.Value);
        Assert.True(apiResponse.Success);
    }

    [Fact]
    public async Task Register_WithPasswordHashingException_ReturnsInternalServerError()
    {
        SetupAuthenticatedUser();
        var registerRequest = RequestTestBuilder.CreateValidRegisterRequest();

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(registerRequest.Username))
            .ReturnsAsync((AuthUser?)null);
        _mockUserRepository.Setup(x => x.GetByEmailAsync(registerRequest.Email))
            .ReturnsAsync((AuthUser?)null);
        _mockPasswordService.Setup(x => x.GenerateRandomSalt())
            .Throws(new InvalidOperationException("Salt generation error"));

        var result = await _controller.Register(registerRequest);

        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusResult.StatusCode);

        var errorResponse = Assert.IsType<ApiErrorResponse>(statusResult.Value);
        Assert.False(errorResponse.Success);

        _mockUserRepository.Verify(x => x.CreateAsync(It.IsAny<AuthUser>()), Times.Never);
    }

    #endregion
}
