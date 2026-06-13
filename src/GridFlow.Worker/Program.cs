using GridFlow.Application.GasFlows;
using GridFlow.Infrastructure;
using GridFlow.Infrastructure.EnergiDataService;
using GridFlow.Worker.Ingestion;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("GridFlow")
    ?? throw new InvalidOperationException(
        "Connection string 'GridFlow' is not configured. Set ConnectionStrings:GridFlow via user-secrets or environment variables.");

builder.Services.AddGridFlowInfrastructure(connectionString);
builder.Services.AddEnergiDataService(builder.Configuration);

builder.Services.AddOptions<IngestionOptions>()
    .Bind(builder.Configuration.GetSection(IngestionOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddScoped<GasFlowIngestionService>();
builder.Services.AddSingleton<IngestionHealthTracker>();
builder.Services.AddHostedService<IngestionWorker>();

builder.Services.AddHealthChecks()
    .AddCheck<IngestionHealthCheck>("ingestion", tags: ["ready"]);

var host = builder.Build();
host.Run();