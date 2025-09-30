# Dockerfile for NumbatWallet.Web.Admin
# Multi-stage build for optimized production image

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files for better caching
COPY ["NumbatWallet.sln", "."]
COPY ["Directory.Packages.props", "."]
COPY ["Directory.Build.props", "."]
COPY ["src/NumbatWallet.Web.Admin/NumbatWallet.Web.Admin.csproj", "src/NumbatWallet.Web.Admin/"]
COPY ["src/NumbatWallet.Application/NumbatWallet.Application.csproj", "src/NumbatWallet.Application/"]
COPY ["src/NumbatWallet.Domain/NumbatWallet.Domain.csproj", "src/NumbatWallet.Domain/"]
COPY ["src/NumbatWallet.Infrastructure/NumbatWallet.Infrastructure.csproj", "src/NumbatWallet.Infrastructure/"]
COPY ["src/NumbatWallet.SharedKernel/NumbatWallet.SharedKernel.csproj", "src/NumbatWallet.SharedKernel/"]
COPY ["src/NumbatWallet.ServiceDefaults/NumbatWallet.ServiceDefaults.csproj", "src/NumbatWallet.ServiceDefaults/"]

# Restore dependencies
RUN dotnet restore "src/NumbatWallet.Web.Admin/NumbatWallet.Web.Admin.csproj"

# Copy everything else
COPY . .

# Build the application
WORKDIR "/src/src/NumbatWallet.Web.Admin"
RUN dotnet build "NumbatWallet.Web.Admin.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "NumbatWallet.Web.Admin.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -g 1000 appuser && \
    useradd -r -u 1000 -g appuser appuser

# Copy published application
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Configure environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Expose port
EXPOSE 8080

# Start the application
ENTRYPOINT ["dotnet", "NumbatWallet.Web.Admin.dll"]