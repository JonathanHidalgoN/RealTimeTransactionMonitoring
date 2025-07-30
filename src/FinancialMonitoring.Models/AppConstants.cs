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

    // API Versioning
    public const string ApiVersion = "1.0";

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

    // Route Helpers
    public static class Routes
    {
        public static string GetVersionedApiPath(string version = ApiVersion) => $"/{ApiRoutePrefix}/v{version}";
        public static string GetTransactionsPath(string version = ApiVersion) => $"{GetVersionedApiPath(version)}/transactions";
        public static string GetTransactionByIdPath(string id, string version = ApiVersion) => $"{GetTransactionsPath(version)}/{id}";
        public static string GetAnomaliesPath(string version = ApiVersion) => $"{GetTransactionsPath(version)}/anomalies";
    }
}
