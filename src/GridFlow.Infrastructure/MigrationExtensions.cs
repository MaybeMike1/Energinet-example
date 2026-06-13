using GridFlow.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GridFlow.Infrastructure;

public static class MigrationExtensions
{
    public static async Task ApplyMigrationsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GridFlowDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}