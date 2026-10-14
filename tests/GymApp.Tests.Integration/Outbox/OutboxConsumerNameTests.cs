using GymApp.Common.Infrastructure.Outbox;
using GymApp.Modules.Scheduling.IntegrationEvents;
using MediatR;

namespace GymApp.Tests.Integration.Outbox;

/// <summary>
/// The outbox consumer ledger must tell apart handlers that share a short type name
/// across modules (Billing / Communication / Gamification all declare a
/// <c>SessionCompletedIntegrationEventHandler</c>).
/// </summary>
public sealed class OutboxConsumerNameTests
{
    // Handlers are internal, so find them by scanning the modules' Application assemblies.
    private static List<Type> SessionCompletedHandlers() =>
        new[] { "Billing", "Communication", "Gamification" }
            .Select(module => System.Reflection.Assembly.Load($"GymApp.Modules.{module}.Application"))
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && typeof(INotificationHandler<SessionCompletedIntegrationEvent>).IsAssignableFrom(t))
            .ToList();

    [Fact]
    public void For_SameNamedHandlersInDifferentModules_AreDistinct()
    {
        var handlers = SessionCompletedHandlers();

        handlers.Count.Should().BeGreaterThan(1, "several modules react to a completed session");
        handlers.Select(h => h.Name).Distinct().Should().HaveCount(1, "they share a short name");
        handlers.Select(OutboxConsumerName.For).Should().OnlyHaveUniqueItems();
        handlers.Select(OutboxConsumerName.For).Should().AllSatisfy(n => n.Length.Should().BeLessOrEqualTo(200));
    }

    [Fact]
    public void IsConsumed_OneNamesakeRan_OthersAreStillPending()
    {
        var handlers = SessionCompletedHandlers();
        var recorded = new[] { OutboxConsumerName.For(handlers[0]) };

        OutboxConsumerName.IsConsumed(handlers[0], recorded, handlers).Should().BeTrue();
        handlers.Skip(1).Should().AllSatisfy(h =>
            OutboxConsumerName.IsConsumed(h, recorded, handlers).Should().BeFalse());
    }

    [Fact]
    public void IsConsumed_LegacyShortNameRow_IgnoredWhenAmbiguous()
    {
        var handlers = SessionCompletedHandlers();
        var legacy = new[] { handlers[0].Name };

        handlers.Should().AllSatisfy(h =>
            OutboxConsumerName.IsConsumed(h, legacy, handlers).Should().BeFalse());
    }

    [Fact]
    public void IsConsumed_LegacyShortNameRow_HonouredWhenUnambiguous()
    {
        var only = new List<Type> { SessionCompletedHandlers()[0] };

        OutboxConsumerName.IsConsumed(only[0], new[] { only[0].Name }, only).Should().BeTrue();
        OutboxConsumerName.IsConsumed(only[0], Array.Empty<string>(), only).Should().BeFalse();
    }
}
