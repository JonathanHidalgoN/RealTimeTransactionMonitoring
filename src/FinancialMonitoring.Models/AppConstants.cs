namespace FinancialMonitoring.Models;

public static class AppConstants
{
    // Configuration Prefixes
    public const string TransactionsTopicName = "transactions";
    public const string AnomaliesTopicName = "anomalies";
    public const string KafkaConfigPrefix = "Kafka";
    public const string ApplicationInsightsConfigPrefix = "ApplicationInsights";
    public const string CosmosDbConfigPrefix = "CosmosDb";
    public const string RedisDbConfigPrefix = "Redis";
    public const string MessagingConfigPrefix = "Messaging";
    public const string EventHubsConfigPrefix = "EventHubs";
    public const string MongoDbConfigPrefix = "MongoDb";
    public const string KafkaDefaultName = "Kafka";
    public const string ApiSettingsConfigPrefix = "ApiSettings";
    public const string CacheSettingsConfigPrefix = "CacheSettings";
    public const string ResponseCacheSettingsConfigPrefix = "ResponseCacheSettings";
    public const string RateLimitSettingsConfigPrefix = "RateLimitSettings";
    public const string JwtSettingsConfigPrefix = "JwtSettings";
    public const string CorsConfigPrefix = "Cors";

    //Env variables names
    public const string runTimeEnvVarName = "DOTNET_ENVIRONMENT";

    // API Versioning
    public const string ApiVersion = "1.0";

    // Default Ports
    public const int DefaultApiPort = 5100;
    public const int DefaultBlazorHttpPort = 5124;
    public const int DefaultBlazorHttpsPort = 7082;
    public const int DefaultMongoDbPort = 27017;

    // Timeouts and Intervals
    public const int DashboardRefreshInterval = 10_000;
    public const int DefaultRequestTimeout = 30_000;

    // HTTP Headers
    public const string CorrelationIdHeader = "X-Correlation-Id";
    public const string ApiKeyHeader = "X-Api-Key";
    public const string ApiVersionHeader = "X-Version";

    // API Routes
    public const string ApiRoutePrefix = "api";
    public const string TransactionsRouteTemplate = "api/v{version:apiVersion}/transactions";
    public const string TransactionByIdRoute = "{id}";
    public const string AnomaliesRoute = "anomalies";

    // Health Check Endpoints
    public const string HealthCheckEndpoint = "/healthz";
    public const string DetailedHealthCheckEndpoint = "/health";

    // Health Check Names
    public const string ApiHealthCheckName = "api";
    public const string DatabaseHealthCheckName = "database";

    // Cache Policy Names
    public const string TransactionCachePolicy = "TransactionCache";
    public const string TransactionByIdCachePolicy = "TransactionByIdCache";
    public const string AnomalousTransactionCachePolicy = "AnomalousTransactionCache";
    public const string AnalyticsCachePolicy = "AnalyticsCache";
    public const string TimeSeriesCachePolicy = "TimeSeriesCache";

    // Authorization Role Names
    public const string AdminRole = "Admin";
    public const string AnalystRole = "Analyst";
    public const string ViewerRole = "Viewer";

    // Common combinations
    public const string AdminAnalystRoles = $"{AdminRole},{AnalystRole}";
    public const string AllRoles = $"{AdminRole},{AnalystRole},{ViewerRole}";

    // Route Helpers
    public static class Routes
    {
        public static string GetVersionedApiPath(string version = ApiVersion) => $"/{ApiRoutePrefix}/v{version}";
        public static string GetTransactionsPath(string version = ApiVersion) => $"{GetVersionedApiPath(version)}/transactions";
        public static string GetTransactionByIdPath(string id, string version = ApiVersion) => $"{GetTransactionsPath(version)}/{id}";
        public static string GetAnomaliesPath(string version = ApiVersion) => $"{GetTransactionsPath(version)}/anomalies";
    }
}
