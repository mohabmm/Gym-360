# syntax=docker/dockerfile:1.7-labs

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution + central MSBuild config (needed for restore)
COPY GymApp.sln Directory.Build.props Directory.Packages.props global.json nuget.config ./

# Copy every csproj first for layer-cached restore.
# --parents preserves the source directory tree (plain COPY globs flatten it);
# the ** glob auto-picks up new projects so this list can never drift.
COPY --parents src/**/*.csproj ./

# Restore only API project graph (skips test projects → faster, smaller restore)
# BuildKit cache mount: NuGet packages persist across builds, skip re-download
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore src/API/GymApp.Api/GymApp.Api.csproj

# Copy source and publish (publish includes build; no separate build stage needed)
COPY src/ src/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish src/API/GymApp.Api/GymApp.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# curl for HEALTHCHECK probe
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_TieredPGO=1 \
    DOTNET_TC_QuickJitForLoops=1 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

# Non-root user (APP_UID=1654 baked into aspnet:8.0 image)
USER $APP_UID

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -fsS http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "GymApp.Api.dll"]
