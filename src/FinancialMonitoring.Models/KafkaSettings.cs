using System.ComponentModel.DataAnnotations;

public class KafkaSettings
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Kafka:BootstrapServers is required and cannot be empty. Check Key Vault secret 'Kafka--BootstrapServers' or ENV var 'Kafka__BootstrapServers'.")]
    public string? BootstrapServers { get; set; }
}
