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

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "GridFlow.Worker.dll"]
