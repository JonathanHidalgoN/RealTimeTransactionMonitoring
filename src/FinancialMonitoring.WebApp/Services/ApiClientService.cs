using FinancialMonitoring.Models;
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
        var requestUri = $"api/v1.0/transactions?pageNumber={pageNumber}&pageSize={pageSize}";
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
        var requestUri = $"api/v1.0/transactions/anomalies?pageNumber={pageNumber}&pageSize={pageSize}";
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
}
