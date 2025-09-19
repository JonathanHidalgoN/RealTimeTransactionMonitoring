using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FinancialMonitoring.Api.Extensions.ServiceRegistration;
using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Api.Services;
using FinancialMonitoring.Models;
using Xunit;

namespace FinancialMonitoring.Api.Tests.Extensions;

public class DataAccessExtensionsTests
{
    [Fact]
    public void AddDataAccess_Production_ShouldRegisterCosmosDbServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=test-key",
                ["CosmosDb:ConnectionString"] = "AccountEndpoint=https://test.documents.azure.com:443/",
                ["CosmosDb:DatabaseName"] = "TestDb",
                ["CosmosDb:ApplicationName"] = "TestApp"
            })
            .Build();

        services.AddDataAccess(configuration, RunTimeEnvironment.Production);

        var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetService<ICosmosDbService>().Should().NotBeNull();
        serviceProvider.GetService<ITransactionQueryService>().Should().NotBeNull();
        serviceProvider.GetService<ITransactionRepository>().Should().NotBeNull();
        serviceProvider.GetService<IAnalyticsRepository>().Should().NotBeNull();

        serviceProvider.GetService<ITransactionQueryService>().Should().BeOfType<CosmosDbTransactionQueryService>();
        serviceProvider.GetService<IAnalyticsRepository>().Should().BeOfType<CosmosDbAnalyticsRepository>();
    }

    [Fact]
    public void AddDataAccess_Development_ShouldRegisterMongoDbServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=test-key",
                ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
                ["MongoDb:DatabaseName"] = "TestDb"
            })
            .Build();

        services.AddDataAccess(configuration, RunTimeEnvironment.Development);

        var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetService<ITransactionRepository>().Should().NotBeNull();
        serviceProvider.GetService<IAnalyticsRepository>().Should().NotBeNull();

        serviceProvider.GetService<IAnalyticsRepository>().Should().BeOfType<MongoAnalyticsRepository>();
    }

    [Fact]
    public void AddDataAccess_Testing_ShouldNotAddApplicationInsights()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
                ["MongoDb:DatabaseName"] = "TestDb",
                ["ASPNETCORE_ENVIRONMENT"] = "Testing"
            })
            .Build();

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        try
        {
            services.AddDataAccess(configuration, RunTimeEnvironment.Development);

            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.GetService<ITransactionRepository>().Should().NotBeNull();
            serviceProvider.GetService<IAnalyticsRepository>().Should().NotBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        }
    }

    [Theory]
    [InlineData(RunTimeEnvironment.Production)]
    [InlineData(RunTimeEnvironment.Development)]
    public void AddDataAccess_ShouldRegisterRequiredOptions(RunTimeEnvironment environment)
    {
        var services = new ServiceCollection();
        var configData = new Dictionary<string, string?>
        {
            ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
            ["MongoDb:DatabaseName"] = "TestDb"
        };

        if (environment == RunTimeEnvironment.Production)
        {
            configData.Add("ApplicationInsights:ConnectionString", "InstrumentationKey=test-key");
            configData.Add("CosmosDb:ConnectionString", "AccountEndpoint=https://test.documents.azure.com:443/");
            configData.Add("CosmosDb:DatabaseName", "TestDb");
            configData.Add("CosmosDb:ApplicationName", "TestApp");
        }
        else
        {
            configData.Add("ApplicationInsights:ConnectionString", "InstrumentationKey=test-key");
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        services.AddDataAccess(configuration, environment);

        var serviceProvider = services.BuildServiceProvider();

        if (environment == RunTimeEnvironment.Production)
        {
            serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<ApplicationInsightsSettings>>().Should().NotBeNull();
            serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<CosmosDbSettings>>().Should().NotBeNull();
        }
        else
        {
            serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<MongoDbSettings>>().Should().NotBeNull();
        }
    }

    [Fact]
    public void AddDataAccess_ShouldReturnServiceCollection()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
                ["MongoDb:DatabaseName"] = "TestDb"
            })
            .Build();

        var result = services.AddDataAccess(configuration, RunTimeEnvironment.Development);

        result.Should().BeSameAs(services);
    }
}