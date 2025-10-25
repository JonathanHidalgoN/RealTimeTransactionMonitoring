using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using FinancialMonitoring.Abstractions;
using FinancialMonitoring.Api.Services;
using FinancialMonitoring.Models;

namespace FinancialMonitoring.IntegrationTests.ApiContracts.Security;

/// <summary>
/// Integration tests for AuthController endpoints
/// Tests the complete authentication flow including login, register, refresh, logout, and user info retrieval
/// </summary>
public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private const string TestSecretKey = "YourSuperSecretKeyForDevelopmentOnlyMustBe32CharsOrMore!";
    private const string TestIssuer = "FinancialMonitoring.Api";
    private const string TestAudience = "FinancialMonitoring.Clients";

    // Test user credentials (from InMemoryUserRepository)
    private const string AdminUsername = "admin";
    private const string AdminPassword = "Admin123!";
    private const string AnalystUsername = "analyst";
    private const string AnalystPassword = "Analyst123!";
    private const string ViewerUsername = "viewer";
    private const string ViewerPassword = "Viewer123!";

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "JwtSettings:SecretKey", TestSecretKey },
                    { "JwtSettings:Issuer", TestIssuer },
                    { "JwtSettings:Audience", TestAudience },
                    { "JwtSettings:AccessTokenExpiryMinutes", "15" },
                    { "JwtSettings:RefreshTokenExpiryDays", "7" },
                    { "JwtSettings:ValidateIssuer", "true" },
                    { "JwtSettings:ValidateAudience", "true" },
                    { "JwtSettings:ValidateLifetime", "true" },
                    { "JwtSettings:ClockSkewMinutes", "5" }
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPasswordHashingService>();
                services.AddSingleton<IPasswordHashingService, PasswordHashingService>();
                services.RemoveAll<IJwtTokenService>();
                services.AddScoped<IJwtTokenService, JwtTokenService>();
                services.RemoveAll<IUserRepository>();
                services.AddSingleton<IUserRepository, InMemoryUserRepository>();
            });
        });
    }


    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnSuccessWithTokens()
    {
        var client = _factory.CreateClient();
        var loginRequest = new
        {
            Username = AdminUsername,
            Password = AdminPassword
        };

        var response = await client.PostAsJsonAsync("/api/v2/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<LoginResponseWrapper>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.AccessToken);
        Assert.NotEmpty(result.Data.RefreshToken);
        Assert.NotNull(result.Data.User);
        Assert.Equal(AdminUsername, result.Data.User.Username);
    }

    [Fact]
    public async Task Login_WithInvalidUsername_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var loginRequest = new
        {
            Username = "nonexistent",
            Password = AdminPassword
        };

        var response = await client.PostAsJsonAsync("/api/v2/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var loginRequest = new
        {
            Username = AdminUsername,
            Password = "WrongPassword!"
        };

        var response = await client.PostAsJsonAsync("/api/v2/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithMissingCredentials_ShouldReturnBadRequest()
    {
        var client = _factory.CreateClient();
        var loginRequest = new
        {
            Username = "",
            Password = ""
        };

        var response = await client.PostAsJsonAsync("/api/v2/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithDifferentRoles_ShouldReturnCorrectRoleInToken()
    {
        var client = _factory.CreateClient();
        var testCases = new[]
        {
            (AdminUsername, AdminPassword, AuthUserRole.Admin),
            (AnalystUsername, AnalystPassword, AuthUserRole.Analyst),
            (ViewerUsername, ViewerPassword, AuthUserRole.Viewer)
        };

        foreach (var (username, password, expectedRole) in testCases)
        {
            var loginRequest = new
            {
                Username = username,
                Password = password
            };

            var response = await client.PostAsJsonAsync("/api/v2/auth/login", loginRequest);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<LoginResponseWrapper>();
            Assert.NotNull(result?.Data);
            Assert.Equal((int)expectedRole, result.Data.User.Role);
        }
    }



    [Fact]
    public async Task RefreshToken_WithValidToken_ShouldReturnNewTokens()
    {
        var client = _factory.CreateClient();

        var loginRequest = new { Username = AdminUsername, Password = AdminPassword };
        var loginResponse = await client.PostAsJsonAsync("/api/v2/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseWrapper>();

        Assert.NotNull(loginResult?.Data?.RefreshToken);

        var refreshRequest = new { RefreshToken = loginResult.Data.RefreshToken };
        var refreshResponse = await client.PostAsJsonAsync("/api/v2/auth/refresh", refreshRequest);

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<RefreshTokenResponseWrapper>();
        Assert.NotNull(refreshResult);
        Assert.True(refreshResult.Success);
        Assert.NotNull(refreshResult.Data);
        Assert.NotEmpty(refreshResult.Data.AccessToken);
        Assert.NotEmpty(refreshResult.Data.RefreshToken);

        Assert.NotEqual(loginResult.Data.AccessToken, refreshResult.Data.AccessToken);
        Assert.NotEqual(loginResult.Data.RefreshToken, refreshResult.Data.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var refreshRequest = new { RefreshToken = "invalid-refresh-token" };

        var response = await client.PostAsJsonAsync("/api/v2/auth/refresh", refreshRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithUsedToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();

        var loginRequest = new { Username = AdminUsername, Password = AdminPassword };
        var loginResponse = await client.PostAsJsonAsync("/api/v2/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseWrapper>();

        var refreshRequest = new { RefreshToken = loginResult!.Data!.RefreshToken };
        await client.PostAsJsonAsync("/api/v2/auth/refresh", refreshRequest);

        var response = await client.PostAsJsonAsync("/api/v2/auth/refresh", refreshRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }



    [Fact]
    public async Task GetMe_WithValidToken_ShouldReturnUserInfo()
    {
        var client = _factory.CreateClient();

        var loginRequest = new { Username = AdminUsername, Password = AdminPassword };
        var loginResponse = await client.PostAsJsonAsync("/api/v2/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseWrapper>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult!.Data!.AccessToken);

        var response = await client.GetAsync("/api/v2/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<UserInfoResponseWrapper>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(AdminUsername, result.Data.Username);
    }

    [Fact]
    public async Task GetMe_WithoutToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v2/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithInvalidToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        var response = await client.GetAsync("/api/v2/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }



    [Fact]
    public async Task Logout_WithValidToken_ShouldInvalidateRefreshToken()
    {
        var client = _factory.CreateClient();

        var loginRequest = new { Username = AdminUsername, Password = AdminPassword };
        var loginResponse = await client.PostAsJsonAsync("/api/v2/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseWrapper>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult!.Data!.AccessToken);

        var logoutRequest = new { RefreshToken = loginResult.Data.RefreshToken };
        var logoutResponse = await client.PostAsJsonAsync("/api/v2/auth/logout", logoutRequest);

        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        var refreshRequest = new { RefreshToken = loginResult.Data.RefreshToken };
        var refreshResponse = await client.PostAsJsonAsync("/api/v2/auth/refresh", refreshRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutToken_ShouldReturnUnauthorized()
    {
        var client = _factory.CreateClient();
        var logoutRequest = new { RefreshToken = "some-token" };

        var response = await client.PostAsJsonAsync("/api/v2/auth/logout", logoutRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }



    [Fact]
    public async Task Register_AsAdmin_ShouldCreateNewUser()
    {
        var client = _factory.CreateClient();

        var loginRequest = new { Username = AdminUsername, Password = AdminPassword };
        var loginResponse = await client.PostAsJsonAsync("/api/v2/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseWrapper>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult!.Data!.AccessToken);

        var registerRequest = new
        {
            Username = "newuser",
            Email = "newuser@test.com",
            Password = "NewUser123!",
            Role = (int)AuthUserRole.Analyst,
            FirstName = "New",
            LastName = "User"
        };

        var response = await client.PostAsJsonAsync("/api/v2/auth/register", registerRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_AsNonAdmin_ShouldReturnForbidden()
    {
        var client = _factory.CreateClient();

        var loginRequest = new { Username = ViewerUsername, Password = ViewerPassword };
        var loginResponse = await client.PostAsJsonAsync("/api/v2/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseWrapper>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult!.Data!.AccessToken);

        var registerRequest = new
        {
            Username = "unauthorized",
            Email = "unauthorized@test.com",
            Password = "Test123!",
            Role = (int)AuthUserRole.Viewer,
            FirstName = "Test",
            LastName = "User"
        };

        var response = await client.PostAsJsonAsync("/api/v2/auth/register", registerRequest);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ShouldReturnConflict()
    {
        var client = _factory.CreateClient();

        var loginRequest = new { Username = AdminUsername, Password = AdminPassword };
        var loginResponse = await client.PostAsJsonAsync("/api/v2/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseWrapper>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult!.Data!.AccessToken);

        var registerRequest = new
        {
            Username = AdminUsername, // Already exists
            Email = "different@test.com",
            Password = "Test123!",
            Role = (int)AuthUserRole.Viewer,
            FirstName = "Test",
            LastName = "User"
        };

        var response = await client.PostAsJsonAsync("/api/v2/auth/register", registerRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }



    private class LoginResponseWrapper
    {
        public bool Success { get; set; }
        public LoginData? Data { get; set; }
    }

    private class LoginData
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserInfo User { get; set; } = null!;
    }

    private class UserInfo
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Role { get; set; }
    }

    private class RefreshTokenResponseWrapper
    {
        public bool Success { get; set; }
        public RefreshTokenData? Data { get; set; }
    }

    private class RefreshTokenData
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    private class UserInfoResponseWrapper
    {
        public bool Success { get; set; }
        public UserInfo? Data { get; set; }
    }

}
