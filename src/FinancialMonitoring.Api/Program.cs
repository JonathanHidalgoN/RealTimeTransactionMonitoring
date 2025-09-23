using FinancialMonitoring.Api.Extensions.Configuration;
using FinancialMonitoring.Api.Extensions.ServiceRegistration;
using FinancialMonitoring.Api.Extensions.Middleware;
using FinancialMonitoring.Models.Extensions;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var environment = EnvironmentDetector.DetectAndConfigureEnvironment(builder);
        var portSettings = builder.Configuration.BuildPortSettings();

        ConfigurationValidator.ValidateConfiguration(builder.Configuration);

        builder.Services.AddCaching(builder.Configuration);
        builder.Services.AddCorsConfiguration(builder.Configuration, portSettings);
        builder.Services.AddRateLimiting(builder.Configuration);
        builder.Services.AddAuthenticationServices(builder.Configuration);
        builder.Services.AddApplicationServices(builder.Configuration);
        builder.Services.AddSwaggerDocumentation();
        builder.Services.AddDataAccess(builder.Configuration, environment);

        var app = builder.Build();
        app.ConfigureMiddleware();

        app.Run();
    }

}

public partial class Program { }
