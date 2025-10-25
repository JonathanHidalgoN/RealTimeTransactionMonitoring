using System.Net.Http.Json;
using System.Text.Json;
using System.Globalization;
using Microsoft.JSInterop;

namespace FinancialMonitoring.WebApp.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private const string AccessTokenKey = "accessToken";
    private const string RefreshTokenKey = "refreshToken";
    private const string TokenExpirationKey = "tokenExpiration";

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public async Task<(bool Success, string? ErrorMessage)> LoginAsync(string username, string password)
    {
        try
        {
            var loginRequest = new
            {
                Username = username,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync("/api/v2/auth/login", loginRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return (false, "Invalid username or password");
            }

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseWrapper>();

            if (loginResponse?.Data == null)
            {
                return (false, "Invalid response from server");
            }

            // Store tokens in localStorage
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, loginResponse.Data.AccessToken);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, loginResponse.Data.RefreshToken);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenExpirationKey, loginResponse.Data.ExpiresAt.ToString("O"));

            return (true, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
            return (false, "An error occurred during login");
        }
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);

            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            var expirationString = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenExpirationKey);

            if (string.IsNullOrEmpty(expirationString))
            {
                return token;
            }

            // Parse with roundtrip format to preserve UTC timezone
            var expiration = DateTime.ParseExact(expirationString, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

            // If token expires in less than 5 minutes, refresh it
            if (expiration < DateTime.UtcNow.AddMinutes(5))
            {
                var refreshed = await RefreshTokenAsync();
                if (refreshed)
                {
                    return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);
                }

                // If refresh failed, return null (user needs to login again)
                return null;
            }

            return token;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting access token: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var refreshToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);

            if (string.IsNullOrEmpty(refreshToken))
            {
                return false;
            }

            var refreshRequest = new
            {
                RefreshToken = refreshToken
            };

            var response = await _httpClient.PostAsJsonAsync("/api/v2/auth/refresh", refreshRequest);

            if (!response.IsSuccessStatusCode)
            {
                // Refresh token is invalid or expired, clear everything
                await LogoutAsync();
                return false;
            }

            var refreshResponse = await response.Content.ReadFromJsonAsync<RefreshTokenResponseWrapper>();

            if (refreshResponse?.Data == null)
            {
                return false;
            }

            // Update tokens
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", AccessTokenKey, refreshResponse.Data.AccessToken);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, refreshResponse.Data.RefreshToken);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenExpirationKey, refreshResponse.Data.ExpiresAt.ToString("O"));

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Token refresh error: {ex.Message}");
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            var refreshToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);

            if (!string.IsNullOrEmpty(refreshToken))
            {
                try
                {
                    var accessToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AccessTokenKey);

                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        _httpClient.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                        var logoutRequest = new { RefreshToken = refreshToken };
                        await _httpClient.PostAsJsonAsync("/api/v2/auth/logout", logoutRequest);
                    }
                }
                catch
                {
                    // Ignore errors during logout API call
                }
            }
        }
        finally
        {
            // Always clear local storage
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", AccessTokenKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenExpirationKey);
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetAccessTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    // Response wrapper classes
    private class LoginResponseWrapper
    {
        public LoginResponseData? Data { get; set; }
    }

    private class LoginResponseData
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    private class RefreshTokenResponseWrapper
    {
        public RefreshTokenResponseData? Data { get; set; }
    }

    private class RefreshTokenResponseData
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
