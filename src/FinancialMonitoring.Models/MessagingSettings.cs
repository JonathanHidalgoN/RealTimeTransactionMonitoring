using System.ComponentModel.DataAnnotations;
using FinancialMonitoring.Models;

public class MessagingSettings
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Provider is required and cannot be empty.")]
    public string Provider { get; set; } = AppConstants.KafkaDefaultName;
}
