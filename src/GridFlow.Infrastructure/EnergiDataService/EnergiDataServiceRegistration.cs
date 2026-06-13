using System.Net;

using GridFlow.Application.GasFlows;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

using Polly;
using Polly.Retry;

namespace GridFlow.Infrastructure.EnergiDataService;

public static class EnergiDataServiceRegistration
{
    public static IHttpClientBuilder AddEnergiDataService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EnergiDataServiceOptions>()
            .Bind(configuration.GetSection(EnergiDataServiceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);

        var httpClientBuilder = services.AddHttpClient<IGasFlowSource, EnergiDataServiceClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<EnergiDataServiceOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);

            // The resilience pipeline owns the per-attempt timeout, so disable HttpClient's own timeout.
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        httpClientBuilder.AddResilienceHandler("energi-data-service", (builder, context) =>
        {
            var options = context.ServiceProvider.GetRequiredService<IOptions<EnergiDataServiceOptions>>().Value;

            builder.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = options.MaxRetries,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = options.BaseRetryDelay,
                ShouldHandle = static args => ValueTask.FromResult(
                    args.Outcome.Result is { StatusCode: HttpStatusCode.TooManyRequests }
                    || HttpClientResiliencePredicates.IsTransient(args.Outcome)),
                DelayGenerator = static args =>
                {
                    // Respect the API's "try again in X" hint on HTTP 429 when present.
                    var retryAfter = args.Outcome.Result?.Headers.RetryAfter;
                    if (retryAfter is not null)
                    {
                        if (retryAfter.Delta is { } delta)
                        {
                            return ValueTask.FromResult<TimeSpan?>(delta);
                        }

                        if (retryAfter.Date is { } date)
                        {
                            return ValueTask.FromResult<TimeSpan?>(date - DateTimeOffset.UtcNow);
                        }
                    }

                    // Fall back to the strategy's jittered exponential backoff.
                    return ValueTask.FromResult<TimeSpan?>(null);
                },
            });

            builder.AddTimeout(options.RequestTimeout);
        });

        return httpClientBuilder;
    }
}