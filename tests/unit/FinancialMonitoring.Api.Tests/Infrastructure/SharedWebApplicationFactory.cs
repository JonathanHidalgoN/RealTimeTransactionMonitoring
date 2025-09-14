using Microsoft.AspNetCore.Mvc.Testing;
using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Microsoft.Extensions.Configuration;
using FinancialMonitoring.Api.Services;
using FinancialMonitoring.Models;
using Microsoft.AspNetCore.Hosting;

namespace FinancialMonitoring.Api.Tests.Infrastructure;

/// <summary>
/// Shared test fixture that provides a configured WebApplicationFactory for integration tests.
/// Uses composition pattern instead of inheritance for better design.
/// </summary>
public class SharedWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    public Mock<ITransactionRepository> MockRepository { get; }
    public const string TestApiKey = "test-api-key-123";

    public SharedWebApplicationFactory()
    {
        MockRepository = new Mock<ITransactionRepository>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            // Load test configuration from JSON file
            var testConfigPath = Path.Combine(AppContext.BaseDirectory, "appsettings.TestDefaults.json");
            configBuilder.AddJsonFile(testConfigPath, optional: false, reloadOnChange: false);

            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ApiSettings:ApiKey", TestApiKey } // Ensure test API key is always consistent
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ITransactionRepository>();
            services.AddSingleton<ITransactionRepository>(MockRepository.Object);

            services.RemoveAll<IPasswordHashingService>();
            services.AddSingleton<IPasswordHashingService, PasswordHashingService>();
            services.RemoveAll<IJwtTokenService>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();

            services.Configure<JwtSettings>(options =>
            {
                options.SecretKey = "test-secret-key-that-is-very-long-for-hmac-sha256";
                options.Issuer = "TestIssuer";
                options.Audience = "TestAudience";
                options.AccessTokenExpiryMinutes = 15;
                options.RefreshTokenExpiryDays = 7;
            });
        });

        base.ConfigureWebHost(builder);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            MockRepository?.Reset();
        }
        base.Dispose(disposing);
    }
}
