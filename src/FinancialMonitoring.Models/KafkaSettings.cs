using System.ComponentModel.DataAnnotations;

namespace FinancialMonitoring.Models;

public class KafkaSettings
{
    [Required(AllowEmptyStrings = false)]
    public string? BootstrapServers { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string? ConsumerGroupId { get; set; } = "transaction-processor-group";
}
