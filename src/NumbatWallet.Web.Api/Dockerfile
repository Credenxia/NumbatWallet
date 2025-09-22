# Stage 1: Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["NumbatWallet.sln", "./"]
COPY ["src/NumbatWallet.SharedKernel/NumbatWallet.SharedKernel.csproj", "src/NumbatWallet.SharedKernel/"]
COPY ["src/NumbatWallet.Domain/NumbatWallet.Domain.csproj", "src/NumbatWallet.Domain/"]
COPY ["src/NumbatWallet.Application/NumbatWallet.Application.csproj", "src/NumbatWallet.Application/"]
COPY ["src/NumbatWallet.Infrastructure/NumbatWallet.Infrastructure.csproj", "src/NumbatWallet.Infrastructure/"]
COPY ["src/NumbatWallet.Web.Api/NumbatWallet.Web.Api.csproj", "src/NumbatWallet.Web.Api/"]
COPY ["src/NumbatWallet.ServiceDefaults/NumbatWallet.ServiceDefaults.csproj", "src/NumbatWallet.ServiceDefaults/"]

# Restore dependencies
RUN dotnet restore "src/NumbatWallet.Web.Api/NumbatWallet.Web.Api.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/src/NumbatWallet.Web.Api"
RUN dotnet build "NumbatWallet.Web.Api.csproj" -c Release -o /app/build

# Stage 2: Publish stage
FROM build AS publish
RUN dotnet publish "NumbatWallet.Web.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published application
COPY --from=publish /app/publish .

# Configure environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Expose ports
EXPOSE 8080

# Switch to non-root user
USER appuser

# Set entrypoint
ENTRYPOINT ["dotnet", "NumbatWallet.Web.Api.dll"]