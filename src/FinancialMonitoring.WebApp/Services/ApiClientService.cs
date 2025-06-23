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

    public async Task<List<Transaction>?> GetTransactionsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<Transaction>>("api/transactions");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching transactions: {ex.Message}");
            return new List<Transaction>();
        }
    }

    public async Task<List<Transaction>?> GetAnomaliesAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<Transaction>>("api/transactions/anomalies");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching anomalies: {ex.Message}");
            return new List<Transaction>();
        }
    }
}
