using System.ComponentModel.DataAnnotations;

namespace FinancialMonitoring.Models;

public class ApiSettings
{
    [Required(AllowEmptyStrings = false)]
    public string? ApiKey { get; set; }
}