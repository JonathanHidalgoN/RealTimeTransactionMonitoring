using FinancialMonitoring.Models;
using FinancialMonitoring.Models.Analytics;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;

namespace FinancialMonitoring.WebApp.Services;

public class ApiClientService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;
    private readonly NavigationManager _navigationManager;

    public ApiClientService(HttpClient httpClient, AuthService authService, NavigationManager navigationManager)
    {
        _httpClient = httpClient;
        _authService = authService;
        _navigationManager = navigationManager;
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        var token = await _authService.GetAccessTokenAsync();

        if (string.IsNullOrEmpty(token))
        {
            // Redirect to login if no valid token
            _navigationManager.NavigateTo("/login");
            throw new UnauthorizedAccessException("No valid authentication token");
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _httpClient;
    }

    public async Task<PagedResult<Transaction>?> GetTransactionsAsync(int pageNumber = 1, int pageSize = 20)
    {
        var requestUri = $"/api/v2/transactions?pageNumber={pageNumber}&pageSize={pageSize}";
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var apiResponse = await client.GetFromJsonAsync<ApiResponse<PagedResult<Transaction>>>(requestUri);
            return apiResponse?.Data;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching transactions: {ex.Message}");
            return null;
        }
    }

    public async Task<PagedResult<Transaction>?> GetAnomaliesAsync(int pageNumber = 1, int pageSize = 20)
    {
        var requestUri = $"/api/v2/transactions/anomalies?pageNumber={pageNumber}&pageSize={pageSize}";
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var apiResponse = await client.GetFromJsonAsync<ApiResponse<PagedResult<Transaction>>>(requestUri);
            return apiResponse?.Data;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching anomalies: {ex.Message}");
            return null;
        }
    }

    // === Analytics API Methods ===

    public async Task<TransactionAnalytics?> GetTransactionAnalyticsAsync()
    {
        var requestUri = "/api/v2/analytics/overview";
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var apiResponse = await client.GetFromJsonAsync<ApiResponse<TransactionAnalytics>>(requestUri);
            return apiResponse?.Data;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching transaction analytics: {ex.Message}");
            return null;
        }
    }

    public async Task<List<TimeSeriesDataPoint>?> GetTransactionTimeSeriesAsync(int hours = 24, int intervalMinutes = 60)
    {
        var requestUri = $"/api/v2/analytics/timeseries/transactions?hours={hours}&intervalMinutes={intervalMinutes}";
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var apiResponse = await client.GetFromJsonAsync<ApiResponse<List<TimeSeriesDataPoint>>>(requestUri);
            return apiResponse?.Data;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching transaction time series: {ex.Message}");
            return null;
        }
    }

    public async Task<List<TimeSeriesDataPoint>?> GetAnomalyTimeSeriesAsync(int hours = 24, int intervalMinutes = 60)
    {
        var requestUri = $"/api/v2/analytics/timeseries/anomalies?hours={hours}&intervalMinutes={intervalMinutes}";
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var apiResponse = await client.GetFromJsonAsync<ApiResponse<List<TimeSeriesDataPoint>>>(requestUri);
            return apiResponse?.Data;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching anomaly time series: {ex.Message}");
            return null;
        }
    }
}
