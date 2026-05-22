using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace GymApp.Tests.Integration.Abstractions;

public sealed class IntegrationTestWebAppFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Integration_Test_P@ss2024!")
        .WithEnvironment("ACCEPT_EULA", "Y")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public IntegrationTestWebAppFactory()
    {
        // Program.cs fail-fasts on a missing/placeholder JWT secret at line ~67,
        // which runs BEFORE builder.Build(). The factory's ConfigureAppConfiguration
        // delegates only execute at Build() time, so an in-memory override is invisible
        // to that pre-Build check. An environment variable, however, IS read by
        // WebApplication.CreateBuilder's eagerly-registered EnvironmentVariables
        // provider. Set it in the ctor — which runs before the host is ever built —
        // so the pre-Build validation sees a valid secret. Double-underscore maps to
        // the Jwt:SecretKey config key.
        Environment.SetEnvironmentVariable(
            "Jwt__SecretKey",
            "integration-tests-only-jwt-secret-key-not-for-production-use");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override connection strings to point to Testcontainers
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = _dbContainer.GetConnectionString(),
                ["ConnectionStrings:Redis"] = _redisContainer.GetConnectionString(),
                // Pin tenancy to Soft mode for integration tests.
                ["Tenancy:Mode"] = "Soft",
                // Jwt:SecretKey is set via env var in the ctor — Program.cs validates
                // it before Build(), so an in-memory override here is too late.
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace Redis IConnectionMultiplexer (it's registered as singleton from the original connection string)
            services.RemoveAll<IConnectionMultiplexer>();
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _redisContainer.StartAsync();

        // EnsureCreated for all DbContexts by resolving them from DI.
        // The WebApplicationFactory will have already built the service provider
        // with the overridden connection strings.
        using var scope = Services.CreateScope();

        // Discover all DbContext-derived services and ensure their databases are created.
        var dbContextDescriptors = scope.ServiceProvider
            .GetServices<DbContextOptions>(); // This won't work for per-context options

        // Instead, iterate all service descriptors that are DbContext types
        // by finding registered DbContext types from the service collection.
        // A simpler approach: resolve known public DbContext types and use EnsureCreated.
        var publicContextTypes = new[]
        {
            typeof(GymApp.Modules.Gyms.Infrastructure.Data.GymsDbContext),
            typeof(GymApp.Modules.Training.Infrastructure.Data.TrainingDbContext),
            typeof(GymApp.Modules.Progress.Infrastructure.Persistence.ProgressDbContext),
            typeof(GymApp.Modules.Scheduling.Infrastructure.Persistence.SchedulingDbContext),
            typeof(GymApp.Modules.Nutrition.Infrastructure.Persistence.NutritionDbContext),
            typeof(GymApp.Modules.Communication.Infrastructure.Data.CommunicationDbContext),
            typeof(GymApp.Modules.Billing.Infrastructure.Data.BillingDbContext),
            typeof(GymApp.Modules.Gamification.Infrastructure.Data.GamificationDbContext),
            typeof(GymApp.Common.Infrastructure.Outbox.OutboxDbContext),
        };


        foreach (var contextType in publicContextTypes)
        {
            if (scope.ServiceProvider.GetService(contextType) is not DbContext dbContext)
                continue;

            if (contextType == typeof(GymApp.Common.Infrastructure.Outbox.OutboxDbContext))
            {
                // EnsureCreatedAsync() no-ops once an earlier context created the
                // shared database, so the outbox schema/table is never made and
                // every event-publishing test fails with
                // "Invalid object name 'outbox.outbox_messages'".
                // OutboxDbContext's model is OutboxMessage only, so per-context
                // CreateTablesAsync() creates exactly outbox.outbox_messages with
                // no cross-context model-merge risk.
                var creator = dbContext.GetService<IRelationalDatabaseCreator>();
                if (!await creator.ExistsAsync())
                    await creator.CreateAsync();
                await creator.CreateTablesAsync();
            }
            else
            {
                await dbContext.Database.EnsureCreatedAsync();
            }
        }

        // Identity DbContext is internal — resolve via its unit-of-work interface.
        var identityUow = scope.ServiceProvider
            .GetService<GymApp.Modules.Identity.Application.Abstractions.IIdentityUnitOfWork>();
        if (identityUow is DbContext identityDbContext)
            await identityDbContext.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _redisContainer.StopAsync();
        await base.DisposeAsync();
    }
}
