using GridFlow.Application.GasFlows;
using GridFlow.Infrastructure;
using GridFlow.Infrastructure.EnergiDataService;
using GridFlow.Infrastructure.Observability;
using GridFlow.Worker.Ingestion;

using Microsoft.Extensions.DependencyInjection.Extensions;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Host.UseGridFlowSerilog("GridFlow.Worker");
}

var connectionString = builder.Configuration.GetConnectionString("GridFlow")
    ?? throw new InvalidOperationException(
        "Connection string 'GridFlow' is not configured. Set ConnectionStrings:GridFlow via user-secrets or environment variables.");

builder.Services.AddGridFlowInfrastructure(connectionString);
builder.Services.AddEnergiDataService(builder.Configuration);
builder.Services.TryAddSingleton(TimeProvider.System);

builder.Services.AddOptions<IngestionOptions>()
    .Bind(builder.Configuration.GetSection(IngestionOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddScoped<GasFlowIngestionService>();
builder.Services.AddSingleton<IngestionHealthTracker>();
builder.Services.AddHostedService<IngestionWorker>();

builder.Services.AddHealthChecks()
    .AddCheck<IngestionHealthCheck>("ingestion", tags: ["ready"]);

var app = builder.Build();

app.UseGridFlowCorrelationId();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseSerilogRequestLogging();
}

app.MapGridFlowHealthEndpoints();

await app.RunAsync();