# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy solution + all .csproj files to restore in one pass (better layer caching)
COPY Cinema.sln .
COPY Cinema.Domain/Cinema.Domain.csproj Cinema.Domain/
COPY Cinema.Application/Cinema.Application.csproj Cinema.Application/
COPY Cinema.Infrastructure/Cinema.Infrastructure.csproj Cinema.Infrastructure/
COPY Cinema.Api/Cinema.Api.csproj Cinema.Api/
COPY Cinema.Tests/Cinema.Tests.csproj Cinema.Tests/

# Restore full solution so analyzer/tool packages are included
RUN dotnet restore Cinema.sln

# Copy all source code
COPY . .

# Publish the API project
RUN dotnet publish Cinema.Api/Cinema.Api.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy published artifacts from build stage
COPY --from=build /app/publish .

# Set ASP.NET Core URLs
ENV ASPNETCORE_URLS=http://+:8080

# Expose port 8080
EXPOSE 8080

# Start the application
ENTRYPOINT ["dotnet", "Cinema.Api.dll"]
