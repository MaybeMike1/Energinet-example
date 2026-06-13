using Microsoft.Extensions.Hosting;

using Serilog;
using Serilog.Formatting.Compact;

namespace GridFlow.Infrastructure.Observability;

public static class GridFlowSerilogExtensions
{
    public static IHostBuilder UseGridFlowSerilog(this IHostBuilder host, string applicationName) =>
        host.UseSerilog(
            (context, _, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", applicationName)
                    .Enrich.WithEnvironmentName()
                    .Enrich.WithMachineName();

                if (UseHumanReadableConsole(context.HostingEnvironment))
                {
                    configuration.WriteTo.Console(
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}");
                }
                else
                {
                    configuration.WriteTo.Console(new RenderedCompactJsonFormatter());
                }
            },
            writeToProviders: false);

    private static bool UseHumanReadableConsole(IHostEnvironment environment) =>
        environment.IsDevelopment()
        || string.Equals(environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase);
}