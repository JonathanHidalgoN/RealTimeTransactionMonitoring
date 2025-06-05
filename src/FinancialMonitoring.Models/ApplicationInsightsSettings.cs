using System.ComponentModel.DataAnnotations;
public class ApplicationInsightsSettings
{

    [Required(AllowEmptyStrings = false, ErrorMessage = "ApplicationInsightsSettings:ConnectionString is required and cannot be empty. Check Key Vault secret 'ApplicationInsights--ConnectionString' or ENV var 'ApplicationInsights__ConectionString'.")]
    public string? ConnectionString { get; set; }
}
