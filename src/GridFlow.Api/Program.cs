using GridFlow.Api.Endpoints;
using GridFlow.Api.Health;
using GridFlow.Application.GasFlows;
using GridFlow.Infrastructure;

using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("GridFlow")
    ?? throw new InvalidOperationException(
        "Connection string 'GridFlow' is not configured. Set ConnectionStrings:GridFlow via user-secrets or environment variables.");

builder.Services.AddGridFlowInfrastructure(connectionString);
builder.Services.TryAddSingleton(TimeProvider.System);

builder.Services.AddOptions<ApiOptions>()
    .Bind(builder.Configuration.GetSection(ApiOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddScoped<GasFlowQueryService>();

builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString, name: "sql", tags: ["ready"])
    .AddCheck<DataFreshnessHealthCheck>("data-freshness", tags: ["ready"]);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("ApplyMigrationsOnStartup"))
{
    await app.Services.ApplyMigrationsAsync();
}

app.MapFlowsEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }))
    .ExcludeFromDescription();

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

app.Run();

// Expose Program for WebApplicationFactory in API tests.
public partial class Program;