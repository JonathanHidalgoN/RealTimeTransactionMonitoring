using System.ComponentModel.DataAnnotations;

namespace FinancialMonitoring.Models;

public class EventHubsSettings
{
    [Required(AllowEmptyStrings = false)]
    public string? ConnectionString { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string? EventHubName { get; set; }

    public string? BlobStorageConnectionString { get; set; }

    public string? BlobContainerName { get; set; }
}
