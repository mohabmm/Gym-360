using GymApp.Common.Infrastructure.Outbox;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Infrastructure.Data;
using GymApp.Modules.Progress.Application.Contracts;
using GymApp.Modules.Training.Application.Commands.LogWorkout;
using GymApp.Modules.Training.Application.DTOs;
using GymApp.Modules.Training.IntegrationEvents;
using GymApp.Tests.Integration.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Tests.Integration.Progress;

/// <summary>
/// Guards the Progress side of a logged workout against the real SqlServer provider:
/// the member's daily workout streak (Home stats, weekly activity, Progress tab) is
/// driven entirely by <see cref="WorkoutLoggedIntegrationEvent"/>. Training never
/// published it, so on staging the streak stayed at 0 after every workout.
/// </summary>
public sealed class WorkoutStreakIntegrationTests : BaseIntegrationTest
{
    public WorkoutStreakIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    private readonly Guid _gymId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private async Task PublishAsync(INotification integrationEvent)
    {
        // Fresh scope per delivery, like the outbox processor.
        using var scope = Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IPublisher>().Publish(integrationEvent);
    }

    private async Task<int> GetStreakAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<IProgressQueryService>();
        return await query.GetWorkoutStreakAsync(_userId, _gymId);
    }

    [Fact]
    public async Task WorkoutLogged_StartsTheDailyWorkoutStreak()
    {
        await PublishAsync(new WorkoutLoggedIntegrationEvent(_userId, _gymId, new DateTime(2026, 10, 14)));

        (await GetStreakAsync()).Should().Be(1);
    }

    [Fact]
    public async Task WorkoutLogged_OnConsecutiveDays_ExtendsTheStreak()
    {
        await PublishAsync(new WorkoutLoggedIntegrationEvent(_userId, _gymId, new DateTime(2026, 10, 13)));
        await PublishAsync(new WorkoutLoggedIntegrationEvent(_userId, _gymId, new DateTime(2026, 10, 14)));

        (await GetStreakAsync()).Should().Be(2);
    }

    [Fact]
    public async Task WorkoutLogged_TwiceOnTheSameDay_CountsOnce()
    {
        var day = new DateTime(2026, 10, 14);

        await PublishAsync(new WorkoutLoggedIntegrationEvent(_userId, _gymId, day));
        await PublishAsync(new WorkoutLoggedIntegrationEvent(_userId, _gymId, day));

        (await GetStreakAsync()).Should().Be(1);
    }

    /// <summary>
    /// Puts the test gym on a plan that includes session logging, the way a real gym
    /// is set up. (Stubbing IGymPlanService needs a second test host, and that host's
    /// Quartz scheduler shuts down the shared outbox job when it is disposed.)
    /// </summary>
    private async Task SubscribeGymToSessionLoggingPlanAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var billing = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        var feature = await billing.Features.AsNoTracking().SingleAsync(f => f.Code == "session_logging");
        var planId = await billing.PlanFeatures.AsNoTracking()
            .Where(pf => pf.FeatureId == feature.Id && pf.IsEnabled)
            .Select(pf => pf.PlanId)
            .FirstAsync();
        billing.GymSubscriptions.Add(GymSubscription.Create(new GymSubscriptionDetails
        {
            GymId = _gymId,
            PlanId = planId
        }));
        await billing.SaveChangesAsync();
    }

    [Fact]
    public async Task LogWorkout_ThroughTheOutbox_StartsTheStreak()
    {
        // Arrange
        await SubscribeGymToSessionLoggingPlanAsync();

        // Act — log a workout the way POST /training/workout-logs does.
        using (var scope = Factory.Services.CreateScope())
        {
            var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new LogWorkoutCommand(
                    UserId: _userId,
                    GymId: _gymId,
                    WorkoutId: null,
                    Date: new DateOnly(2026, 10, 14),
                    StartedAt: DateTime.UtcNow.AddMinutes(-30),
                    CompletedAt: DateTime.UtcNow,
                    DurationMinutes: 30,
                    Notes: null,
                    Sets: new List<LogSetDto> { new(Guid.NewGuid(), 1, 10, 12.5m, "Kg", null, true, null) },
                    PerceivedExertion: 8));
            result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Error.Code : null);
        }

        // Assert — the event is committed to the outbox with the log...
        using (var scope = Factory.Services.CreateScope())
        {
            var outbox = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
            var messages = await outbox.OutboxMessages.AsNoTracking()
                .Where(m => m.Type == nameof(WorkoutLoggedIntegrationEvent))
                .Select(m => m.Content)
                .ToListAsync();
            messages.Should().Contain(c => c.Contains(_userId.ToString()));
        }

        // ...and the outbox job (10 s tick in the test host) delivers it to Progress.
        var deadline = DateTime.UtcNow.AddSeconds(45);
        var streak = 0;
        while (DateTime.UtcNow < deadline && streak == 0)
        {
            streak = await GetStreakAsync();
            if (streak == 0) await Task.Delay(500);
        }
        streak.Should().Be(1);
    }
}
