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

namespace GymApp.Tests.Functional.Abstractions;

public sealed class FunctionalTestWebAppFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Functional_Test_P@ss2024!")
        .WithEnvironment("ACCEPT_EULA", "Y")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public FunctionalTestWebAppFactory()
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
            "functional-tests-only-jwt-secret-key-not-for-production-use");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = _dbContainer.GetConnectionString(),
                ["ConnectionStrings:Redis"] = _redisContainer.GetConnectionString(),
                // Pin tenancy to Soft mode for functional tests — Hard mode
                // requires the bootstrap migration + gyms_registry rows that
                // these tests don't seed.
                ["Tenancy:Mode"] = "Soft",
                // Jwt:SecretKey is set via env var in the ctor — Program.cs validates
                // it before Build(), so an in-memory override here is too late.
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IConnectionMultiplexer>();
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _redisContainer.StartAsync();

        using var scope = Services.CreateScope();

        // Ensure databases exist for all publicly-accessible DbContext types
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
            if (scope.ServiceProvider.GetService(contextType) is DbContext dbContext)
                await EnsureSchemaAsync(dbContext);
        }

        // For internal DbContexts, resolve through their unit-of-work interfaces
        var identityUow = scope.ServiceProvider
            .GetService<GymApp.Modules.Identity.Application.Abstractions.IIdentityUnitOfWork>();
        if (identityUow is DbContext identityDbContext)
            await identityDbContext.Database.EnsureCreatedAsync();
    }

    private static Task EnsureSchemaAsync(DbContext dbContext) =>
        dbContext is GymApp.Common.Infrastructure.Outbox.OutboxDbContext
            ? EnsureOutboxTablesAsync(dbContext)
            : dbContext.Database.EnsureCreatedAsync();

    // EnsureCreatedAsync() no-ops once an earlier context created the
    // shared database, so the outbox schema/table is never made and
    // every event-publishing test fails with
    // "Invalid object name 'outbox.outbox_messages'".
    // OutboxDbContext's model is OutboxMessage only, so per-context
    // CreateTablesAsync() creates exactly outbox.outbox_messages with
    // no cross-context model-merge risk.
    private static async Task EnsureOutboxTablesAsync(DbContext dbContext)
    {
        var creator = dbContext.GetService<IRelationalDatabaseCreator>();
        if (!await creator.ExistsAsync())
            await creator.CreateAsync();
        await creator.CreateTablesAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _redisContainer.StopAsync();
        await base.DisposeAsync();
    }
}
