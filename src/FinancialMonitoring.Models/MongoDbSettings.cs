using System.ComponentModel.DataAnnotations;

namespace FinancialMonitoring.Models;

public class MongoDbSettings
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "MongoDB ConnectionString is required. Check environment variable 'MongoDb__ConnectionString'.")]
    public string ConnectionString { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "MongoDB DatabaseName is required. Check environment variable 'MongoDb__DatabaseName'.")]
    public string DatabaseName { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "MongoDB CollectionName is required. Check environment variable 'MongoDb__CollectionName'.")]
    public string CollectionName { get; set; } = string.Empty;
}