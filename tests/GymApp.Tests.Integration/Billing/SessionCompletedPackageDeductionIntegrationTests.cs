using GymApp.Common.Infrastructure.Outbox;
using GymApp.Modules.Billing.Application.Abstractions;
using GymApp.Modules.Billing.Domain.Entities;
using GymApp.Modules.Billing.Domain.Enums;
using GymApp.Modules.Billing.Domain.Repositories;
using GymApp.Modules.Billing.Domain.ValueObjects;
using GymApp.Modules.Scheduling.IntegrationEvents;
using GymApp.Tests.Integration.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace GymApp.Tests.Integration.Billing;

/// <summary>
/// Guards the Billing side of session completion / late cancellation against the real
/// SqlServer provider: the member's package must lose one session and gain a usage row —
/// both when the handler is invoked directly and when the event travels through the
/// outbox, where Billing's handler shares its short type name with Communication's and
/// Gamification's and used to be skipped as "already consumed" once one of them had run.
/// </summary>
public sealed class SessionCompletedPackageDeductionIntegrationTests : BaseIntegrationTest
{
    public SessionCompletedPackageDeductionIntegrationTests(IntegrationTestWebAppFactory factory)
        : base(factory) { }

    private readonly Guid _gymId = Guid.NewGuid();
    private readonly Guid _trainerId = Guid.NewGuid();
    private readonly Guid _memberId = Guid.NewGuid();

    private async Task<Guid> SeedPackageAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ISessionPackageRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IBillingUnitOfWork>();
        var pkg = SessionPackage.Create(new SessionPackageDetails
        {
            GymId = _gymId,
            TrainerId = _trainerId,
            MemberId = _memberId,
            TemplateId = Guid.NewGuid(),
            Name = "10 Sessions",
            TotalSessions = 10,
            PricePerSession = Money.Create(500m),
            TotalPrice = Money.Create(5000m),
            AssignedBy = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddMonths(3)
        });
        await repo.AddAsync(pkg);
        await uow.SaveChangesAsync();
        return pkg.Id;
    }

    private async Task PublishAsync(INotification integrationEvent)
    {
        // Fresh scope per delivery, like the outbox processor.
        using var scope = Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IPublisher>().Publish(integrationEvent);
    }

    private async Task<SessionPackage> LoadPackageAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ISessionPackageRepository>();
        var pkg = await repo.GetActiveByMemberAndTrainerAsync(_gymId, _memberId, _trainerId);
        pkg.Should().NotBeNull();
        return pkg!;
    }

    // Seeds a package, delivers the event built for a fresh session id, and asserts
    // exactly one session was deducted with a usage row of the expected type.
    private async Task AssertSingleDeductionAsync(
        Func<Guid, INotification> eventForSession, PackageUsageType expectedUsage)
    {
        await SeedPackageAsync();
        var sessionId = Guid.NewGuid();

        await PublishAsync(eventForSession(sessionId));

        var pkg = await LoadPackageAsync();
        pkg.RemainingSessions.Should().Be(9);
        pkg.UsageHistory.Should().ContainSingle(u =>
            u.SessionId == sessionId && u.UsageType == expectedUsage);
    }

    [Fact]
    public Task SessionCompleted_DeductsOneSessionAndRecordsUsage() =>
        AssertSingleDeductionAsync(
            sessionId => new SessionCompletedIntegrationEvent(sessionId, _trainerId, _memberId, _gymId),
            PackageUsageType.Completed);

    [Fact]
    public async Task SessionCompleted_DeliveredTwice_DeductsOnlyOnce()
    {
        await SeedPackageAsync();
        var evt = new SessionCompletedIntegrationEvent(Guid.NewGuid(), _trainerId, _memberId, _gymId);

        await PublishAsync(evt);
        await PublishAsync(evt);

        var pkg = await LoadPackageAsync();
        pkg.RemainingSessions.Should().Be(8 + 1);
        pkg.UsageHistory.Should().HaveCount(1);
    }

    [Fact]
    public Task SessionLateCancelled_DeductsOneSessionAndRecordsUsage() =>
        AssertSingleDeductionAsync(
            sessionId => new SessionLateCancelledIntegrationEvent(
                sessionId, _trainerId, _memberId, _gymId, _memberId),
            PackageUsageType.LateCancelled);

    [Fact]
    public async Task SessionCompleted_DeliveredThroughOutbox_RunsEveryModulesHandler()
    {
        await SeedPackageAsync();
        var sessionId = Guid.NewGuid();
        var evt = new SessionCompletedIntegrationEvent(sessionId, _trainerId, _memberId, _gymId);
        var messageId = Guid.NewGuid();

        using (var scope = Factory.Services.CreateScope())
        {
            var outbox = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
            outbox.OutboxMessages.Add(new OutboxMessage
            {
                Id = messageId,
                // The job drains the oldest 20 unprocessed messages per tick. Back-date this
                // one so it is at the head of the queue even when other tests in the run
                // have left a backlog (otherwise it can wait several ticks behind them).
                OccurredOnUtc = DateTime.UtcNow.AddYears(-10),
                Type = evt.GetType().Name,
                Content = JsonConvert.SerializeObject(
                    evt, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All }),
            });
            await outbox.SaveChangesAsync();
        }

        // The Quartz outbox job in the test host ticks every 10 seconds.
        List<string> consumers = new();
        var deadline = DateTime.UtcNow.AddSeconds(45);
        while (DateTime.UtcNow < deadline)
        {
            using var scope = Factory.Services.CreateScope();
            var outbox = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
            var message = await outbox.OutboxMessages.AsNoTracking().SingleAsync(m => m.Id == messageId);
            if (message.ProcessedOnUtc is not null)
            {
                message.Error.Should().BeNull();
                consumers = await outbox.OutboxMessageConsumers.AsNoTracking()
                    .Where(c => c.OutboxMessageId == messageId)
                    .Select(c => c.Name)
                    .ToListAsync();
                break;
            }
            await Task.Delay(500);
        }

        consumers.Should().HaveCountGreaterThan(1, "every module's handler must be recorded, not just the first");
        consumers.Should().Contain(n => n.StartsWith("GymApp.Modules.Billing."));
        consumers.Should().Contain(n => n.StartsWith("GymApp.Modules.Communication."));

        var pkg = await LoadPackageAsync();
        pkg.RemainingSessions.Should().Be(9);
        pkg.UsageHistory.Should().ContainSingle(u => u.SessionId == sessionId);
    }
}
