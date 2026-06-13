using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GridFlow.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used only by the EF Core tools (e.g. <c>dotnet ef migrations add</c>).
/// The connection string is never opened during migration scaffolding; it just gives the tools a
/// provider to build the model against. Runtime wiring goes through
/// <see cref="DependencyInjection.AddGridFlowInfrastructure"/> with a real connection string.
/// </summary>
public sealed class GridFlowDbContextFactory : IDesignTimeDbContextFactory<GridFlowDbContext>
{
    public GridFlowDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<GridFlowDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=GridFlow;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        return new GridFlowDbContext(options);
    }
}