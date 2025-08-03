using FinancialMonitoring.Models;
using FinancialMonitoring.WebApp.Models;
using System.Net.Http.Json;

namespace FinancialMonitoring.WebApp.Services;

public class ApiClientService
{
    private readonly HttpClient _httpClient;

    public ApiClientService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResult<Transaction>?> GetTransactionsAsync(int pageNumber = 1, int pageSize = 20)
    {
        var requestUri = $"{AppConstants.Routes.GetTransactionsPath()}?pageNumber={pageNumber}&pageSize={pageSize}";
        try
        {
            var apiResponse = await _httpClient.GetFromJsonAsync<ApiResponse<PagedResult<Transaction>>>(requestUri);
            return apiResponse?.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching transactions: {ex.Message}");
            return null;
        }
    }

    public async Task<PagedResult<Transaction>?> GetAnomaliesAsync(int pageNumber = 1, int pageSize = 20)
    {
        var requestUri = $"{AppConstants.Routes.GetAnomaliesPath()}?pageNumber={pageNumber}&pageSize={pageSize}";
        try
        {
            var apiResponse = await _httpClient.GetFromJsonAsync<ApiResponse<PagedResult<Transaction>>>(requestUri);
            return apiResponse?.Data;
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
        var requestUri = "/api/v1/analytics/overview";
        try
        {
            var apiResponse = await _httpClient.GetFromJsonAsync<ApiResponse<TransactionAnalytics>>(requestUri);
            return apiResponse?.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching transaction analytics: {ex.Message}");
            return null;
        }
    }

    public async Task<List<TimeSeriesDataPoint>?> GetTransactionTimeSeriesAsync(int hours = 24, int intervalMinutes = 60)
    {
        var requestUri = $"/api/v1/analytics/timeseries/transactions?hours={hours}&intervalMinutes={intervalMinutes}";
        try
        {
            var apiResponse = await _httpClient.GetFromJsonAsync<ApiResponse<List<TimeSeriesDataPoint>>>(requestUri);
            return apiResponse?.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching transaction time series: {ex.Message}");
            return null;
        }
    }

    public async Task<List<TimeSeriesDataPoint>?> GetAnomalyTimeSeriesAsync(int hours = 24, int intervalMinutes = 60)
    {
        var requestUri = $"/api/v1/analytics/timeseries/anomalies?hours={hours}&intervalMinutes={intervalMinutes}";
        try
        {
            var apiResponse = await _httpClient.GetFromJsonAsync<ApiResponse<List<TimeSeriesDataPoint>>>(requestUri);
            return apiResponse?.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching anomaly time series: {ex.Message}");
            return null;
        }
    }
}
