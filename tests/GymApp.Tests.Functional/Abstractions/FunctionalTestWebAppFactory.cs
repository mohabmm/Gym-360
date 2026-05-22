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

        // For internal DbContexts, resolve through their unit-of-work interfaces
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
