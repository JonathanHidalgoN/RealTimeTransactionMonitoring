using FinancialMonitoring.Abstractions.Persistence;
using FinancialMonitoring.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CosmosDbSettings>(
    builder.Configuration.GetSection("CosmosDb")
);
builder.Services.AddSingleton<ITransactionQueryService, CosmosDbTransactionQueryService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
