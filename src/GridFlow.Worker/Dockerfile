FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/GridFlow.Domain/GridFlow.Domain.csproj", "src/GridFlow.Domain/"]
COPY ["src/GridFlow.Application/GridFlow.Application.csproj", "src/GridFlow.Application/"]
COPY ["src/GridFlow.Infrastructure/GridFlow.Infrastructure.csproj", "src/GridFlow.Infrastructure/"]
COPY ["src/GridFlow.Worker/GridFlow.Worker.csproj", "src/GridFlow.Worker/"]
RUN dotnet restore "src/GridFlow.Worker/GridFlow.Worker.csproj"

COPY . .
RUN dotnet publish "src/GridFlow.Worker/GridFlow.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GridFlow.Worker.dll"]
