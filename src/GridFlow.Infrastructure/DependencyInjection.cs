using GridFlow.Application.GasFlows;
using GridFlow.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GridFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddGridFlowInfrastructure(this IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("A GridFlow connection string is required.", nameof(connectionString));
        }

        services.AddDbContext<GridFlowDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IGasFlowRepository, GasFlowRepository>();

        return services;
    }
}