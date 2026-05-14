# syntax=docker/dockerfile:1.7

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution + central MSBuild config (needed for restore)
COPY GymApp.sln Directory.Build.props Directory.Packages.props global.json nuget.config ./

# Copy csproj files first for layer-cached restore
COPY src/Common/GymApp.Common.Domain/*.csproj src/Common/GymApp.Common.Domain/
COPY src/Common/GymApp.Common.Application/*.csproj src/Common/GymApp.Common.Application/
COPY src/Common/GymApp.Common.Infrastructure/*.csproj src/Common/GymApp.Common.Infrastructure/
COPY src/Common/GymApp.Common.Presentation/*.csproj src/Common/GymApp.Common.Presentation/
COPY src/API/GymApp.Api/*.csproj src/API/GymApp.Api/

COPY src/Modules/Identity/GymApp.Modules.Identity.Domain/*.csproj src/Modules/Identity/GymApp.Modules.Identity.Domain/
COPY src/Modules/Identity/GymApp.Modules.Identity.Application/*.csproj src/Modules/Identity/GymApp.Modules.Identity.Application/
COPY src/Modules/Identity/GymApp.Modules.Identity.Infrastructure/*.csproj src/Modules/Identity/GymApp.Modules.Identity.Infrastructure/
COPY src/Modules/Identity/GymApp.Modules.Identity.Presentation/*.csproj src/Modules/Identity/GymApp.Modules.Identity.Presentation/
COPY src/Modules/Identity/GymApp.Modules.Identity.IntegrationEvents/*.csproj src/Modules/Identity/GymApp.Modules.Identity.IntegrationEvents/

COPY src/Modules/Gyms/GymApp.Modules.Gyms.Domain/*.csproj src/Modules/Gyms/GymApp.Modules.Gyms.Domain/
COPY src/Modules/Gyms/GymApp.Modules.Gyms.Application/*.csproj src/Modules/Gyms/GymApp.Modules.Gyms.Application/
COPY src/Modules/Gyms/GymApp.Modules.Gyms.Infrastructure/*.csproj src/Modules/Gyms/GymApp.Modules.Gyms.Infrastructure/
COPY src/Modules/Gyms/GymApp.Modules.Gyms.Presentation/*.csproj src/Modules/Gyms/GymApp.Modules.Gyms.Presentation/
COPY src/Modules/Gyms/GymApp.Modules.Gyms.IntegrationEvents/*.csproj src/Modules/Gyms/GymApp.Modules.Gyms.IntegrationEvents/

COPY src/Modules/Training/GymApp.Modules.Training.Domain/*.csproj src/Modules/Training/GymApp.Modules.Training.Domain/
COPY src/Modules/Training/GymApp.Modules.Training.Application/*.csproj src/Modules/Training/GymApp.Modules.Training.Application/
COPY src/Modules/Training/GymApp.Modules.Training.Infrastructure/*.csproj src/Modules/Training/GymApp.Modules.Training.Infrastructure/
COPY src/Modules/Training/GymApp.Modules.Training.Presentation/*.csproj src/Modules/Training/GymApp.Modules.Training.Presentation/
COPY src/Modules/Training/GymApp.Modules.Training.IntegrationEvents/*.csproj src/Modules/Training/GymApp.Modules.Training.IntegrationEvents/

COPY src/Modules/Progress/GymApp.Modules.Progress.Domain/*.csproj src/Modules/Progress/GymApp.Modules.Progress.Domain/
COPY src/Modules/Progress/GymApp.Modules.Progress.Application/*.csproj src/Modules/Progress/GymApp.Modules.Progress.Application/
COPY src/Modules/Progress/GymApp.Modules.Progress.Infrastructure/*.csproj src/Modules/Progress/GymApp.Modules.Progress.Infrastructure/
COPY src/Modules/Progress/GymApp.Modules.Progress.Presentation/*.csproj src/Modules/Progress/GymApp.Modules.Progress.Presentation/
COPY src/Modules/Progress/GymApp.Modules.Progress.IntegrationEvents/*.csproj src/Modules/Progress/GymApp.Modules.Progress.IntegrationEvents/

COPY src/Modules/Scheduling/GymApp.Modules.Scheduling.Domain/*.csproj src/Modules/Scheduling/GymApp.Modules.Scheduling.Domain/
COPY src/Modules/Scheduling/GymApp.Modules.Scheduling.Application/*.csproj src/Modules/Scheduling/GymApp.Modules.Scheduling.Application/
COPY src/Modules/Scheduling/GymApp.Modules.Scheduling.Infrastructure/*.csproj src/Modules/Scheduling/GymApp.Modules.Scheduling.Infrastructure/
COPY src/Modules/Scheduling/GymApp.Modules.Scheduling.Presentation/*.csproj src/Modules/Scheduling/GymApp.Modules.Scheduling.Presentation/
COPY src/Modules/Scheduling/GymApp.Modules.Scheduling.IntegrationEvents/*.csproj src/Modules/Scheduling/GymApp.Modules.Scheduling.IntegrationEvents/

COPY src/Modules/Nutrition/GymApp.Modules.Nutrition.Domain/*.csproj src/Modules/Nutrition/GymApp.Modules.Nutrition.Domain/
COPY src/Modules/Nutrition/GymApp.Modules.Nutrition.Application/*.csproj src/Modules/Nutrition/GymApp.Modules.Nutrition.Application/
COPY src/Modules/Nutrition/GymApp.Modules.Nutrition.Infrastructure/*.csproj src/Modules/Nutrition/GymApp.Modules.Nutrition.Infrastructure/
COPY src/Modules/Nutrition/GymApp.Modules.Nutrition.Presentation/*.csproj src/Modules/Nutrition/GymApp.Modules.Nutrition.Presentation/
COPY src/Modules/Nutrition/GymApp.Modules.Nutrition.IntegrationEvents/*.csproj src/Modules/Nutrition/GymApp.Modules.Nutrition.IntegrationEvents/

COPY src/Modules/Communication/GymApp.Modules.Communication.Domain/*.csproj src/Modules/Communication/GymApp.Modules.Communication.Domain/
COPY src/Modules/Communication/GymApp.Modules.Communication.Application/*.csproj src/Modules/Communication/GymApp.Modules.Communication.Application/
COPY src/Modules/Communication/GymApp.Modules.Communication.Infrastructure/*.csproj src/Modules/Communication/GymApp.Modules.Communication.Infrastructure/
COPY src/Modules/Communication/GymApp.Modules.Communication.Presentation/*.csproj src/Modules/Communication/GymApp.Modules.Communication.Presentation/
COPY src/Modules/Communication/GymApp.Modules.Communication.IntegrationEvents/*.csproj src/Modules/Communication/GymApp.Modules.Communication.IntegrationEvents/

COPY src/Modules/Billing/GymApp.Modules.Billing.Domain/*.csproj src/Modules/Billing/GymApp.Modules.Billing.Domain/
COPY src/Modules/Billing/GymApp.Modules.Billing.Application/*.csproj src/Modules/Billing/GymApp.Modules.Billing.Application/
COPY src/Modules/Billing/GymApp.Modules.Billing.Infrastructure/*.csproj src/Modules/Billing/GymApp.Modules.Billing.Infrastructure/
COPY src/Modules/Billing/GymApp.Modules.Billing.Presentation/*.csproj src/Modules/Billing/GymApp.Modules.Billing.Presentation/
COPY src/Modules/Billing/GymApp.Modules.Billing.IntegrationEvents/*.csproj src/Modules/Billing/GymApp.Modules.Billing.IntegrationEvents/

COPY src/Modules/Gamification/GymApp.Modules.Gamification.Domain/*.csproj src/Modules/Gamification/GymApp.Modules.Gamification.Domain/
COPY src/Modules/Gamification/GymApp.Modules.Gamification.Application/*.csproj src/Modules/Gamification/GymApp.Modules.Gamification.Application/
COPY src/Modules/Gamification/GymApp.Modules.Gamification.Infrastructure/*.csproj src/Modules/Gamification/GymApp.Modules.Gamification.Infrastructure/
COPY src/Modules/Gamification/GymApp.Modules.Gamification.Presentation/*.csproj src/Modules/Gamification/GymApp.Modules.Gamification.Presentation/
COPY src/Modules/Gamification/GymApp.Modules.Gamification.IntegrationEvents/*.csproj src/Modules/Gamification/GymApp.Modules.Gamification.IntegrationEvents/

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
