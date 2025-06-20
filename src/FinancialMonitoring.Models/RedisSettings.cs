using System.ComponentModel.DataAnnotations;

namespace FinancialMonitoring.Models;

public class RedisSettings
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "The Redis ConnectionString is required. Check Key Vault secret 'Redis--ConnectionString'.")]
    public string? ConnectionString { get; set; }
}