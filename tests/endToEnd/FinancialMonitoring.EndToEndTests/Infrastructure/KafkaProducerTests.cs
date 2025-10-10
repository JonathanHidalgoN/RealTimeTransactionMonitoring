using System.Text.Json;
using Confluent.Kafka;

namespace FinancialMonitoring.EndToEndTests.Infrastructure;

/// <summary>
/// Kafka producer functionality tests
/// </summary>
[Trait("Category", "Infrastructure")]
public class KafkaProducerTests : IAsyncLifetime
{
    private readonly IntegrationTestConfiguration _config;
    private IProducer<Null, string> _producer = null!;

    public KafkaProducerTests()
    {
        _config = IntegrationTestConfiguration.FromEnvironment();
        _config.Validate();
    }

    public async Task InitializeAsync()
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _config.Kafka.BootstrapServers
        };
        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
        await Task.CompletedTask;
    }

    /// <summary>
    /// This test verifies that the Kafka producer can successfully send messages and receive persistence confirmation
    /// </summary>
    [Fact]
    public async Task KafkaProducer_ShouldSendMessage()
    {
        var testMessage = new { test = "message", timestamp = DateTimeOffset.UtcNow };
        var result = await _producer.ProduceAsync("transactions", new Message<Null, string>
        {
            Value = JsonSerializer.Serialize(testMessage)
        });

        Assert.NotNull(result);
        Assert.Equal(PersistenceStatus.Persisted, result.Status);
    }

    public async Task DisposeAsync()
    {
        _producer?.Dispose();
        await Task.CompletedTask;
    }
}
