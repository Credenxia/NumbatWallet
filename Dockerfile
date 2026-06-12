# NumbatWallet — Multi-stage Dockerfile
# Builds three targets: api (Web.Api), admin (Web.Admin) and migrations (EF Core bundle).
# Mirrors the credentry-infrastructure product pattern (single Dockerfile, named targets).
#
# Usage:
#   docker build --target api        -t numbatwallet-api .
#   docker build --target admin      -t numbatwallet-admin .
#   docker build --target migrations -t numbatwallet-migrations .

# =============================================================================
# Base runtime image — non-root, curl for HEALTHCHECK
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# =============================================================================
# Shared restore stage — copies project files and restores dependencies
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

# Solution-level build files (central package management)
COPY global.json Directory.Build.props Directory.Build.targets Directory.Packages.props ./

# Project files only (preserving structure) so restore layers cache well
COPY src/NumbatWallet.SharedKernel/NumbatWallet.SharedKernel.csproj src/NumbatWallet.SharedKernel/
COPY src/NumbatWallet.Domain/NumbatWallet.Domain.csproj src/NumbatWallet.Domain/
COPY src/NumbatWallet.Application/NumbatWallet.Application.csproj src/NumbatWallet.Application/
COPY src/NumbatWallet.Infrastructure/NumbatWallet.Infrastructure.csproj src/NumbatWallet.Infrastructure/
COPY src/NumbatWallet.ServiceDefaults/NumbatWallet.ServiceDefaults.csproj src/NumbatWallet.ServiceDefaults/
COPY src/NumbatWallet.Web.Api/NumbatWallet.Web.Api.csproj src/NumbatWallet.Web.Api/
COPY src/NumbatWallet.Web.Admin/NumbatWallet.Web.Admin.csproj src/NumbatWallet.Web.Admin/

RUN dotnet restore src/NumbatWallet.Web.Api/NumbatWallet.Web.Api.csproj && \
    dotnet restore src/NumbatWallet.Web.Admin/NumbatWallet.Web.Admin.csproj

# Copy source (filtered by .dockerignore — no bin/obj/tests)
COPY src/NumbatWallet.SharedKernel/ src/NumbatWallet.SharedKernel/
COPY src/NumbatWallet.Domain/ src/NumbatWallet.Domain/
COPY src/NumbatWallet.Application/ src/NumbatWallet.Application/
COPY src/NumbatWallet.Infrastructure/ src/NumbatWallet.Infrastructure/
COPY src/NumbatWallet.ServiceDefaults/ src/NumbatWallet.ServiceDefaults/
COPY src/NumbatWallet.Web.Api/ src/NumbatWallet.Web.Api/
COPY src/NumbatWallet.Web.Admin/ src/NumbatWallet.Web.Admin/

# Workaround: .NET 10 SDK glob expansion fails if bin/Debug doesn't exist (MSB3552).
RUN find src -name "*.csproj" -exec dirname {} \; | while read d; do mkdir -p "$d/bin/Debug/net10.0"; done

# =============================================================================
# API build stage
# =============================================================================
FROM restore AS build-api
RUN dotnet publish src/NumbatWallet.Web.Api/NumbatWallet.Web.Api.csproj \
    -c Release -o /app/api --no-restore

# =============================================================================
# Admin build stage — separate stage so Blazor static web asset generation
# is not polluted by API build artifacts
# =============================================================================
FROM restore AS build-admin
RUN dotnet publish src/NumbatWallet.Web.Admin/NumbatWallet.Web.Admin.csproj \
    -c Release -o /app/admin --no-restore

# =============================================================================
# Migrations build stage — EF Core migrations bundle (framework-dependent,
# executed on the aspnet base image by the Helm pre-install/pre-upgrade Job)
# =============================================================================
FROM restore AS build-migrations
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet ef migrations bundle \
    --project src/NumbatWallet.Infrastructure \
    --startup-project src/NumbatWallet.Infrastructure \
    --configuration Release \
    --output /app/efbundle

# =============================================================================
# API runtime target
# =============================================================================
FROM base AS api
COPY --from=build-api /app/api .
ENV ASPNETCORE_ENVIRONMENT=Production
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -fsS http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "NumbatWallet.Web.Api.dll"]

# =============================================================================
# Admin portal runtime target
# =============================================================================
FROM base AS admin
COPY --from=build-admin /app/admin .
ENV ASPNETCORE_ENVIRONMENT=Production
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -fsS http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "NumbatWallet.Web.Admin.dll"]

# =============================================================================
# Migrations runtime target — runs `./efbundle --connection "$..."`
# =============================================================================
FROM base AS migrations
COPY --from=build-migrations /app/efbundle .
USER $APP_UID
ENTRYPOINT ["./efbundle"]
