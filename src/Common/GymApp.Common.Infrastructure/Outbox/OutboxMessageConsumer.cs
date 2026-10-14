namespace GymApp.Common.Infrastructure.Outbox;

/// <summary>
/// Ledger row recording that a specific handler has already consumed a specific outbox message.
/// Composite key (<see cref="OutboxMessageId"/>, <see cref="Name"/>). The outbox processor checks
/// this before invoking a handler and inserts a row after success, so a retried message only
/// re-runs the handler(s) that previously failed — making at-least-once delivery safe for
/// non-idempotent (e.g. money-touching) handlers.
/// </summary>
public sealed class OutboxMessageConsumer
{
    public Guid OutboxMessageId { get; set; }

    /// <summary>
    /// The consuming handler's namespace-qualified type name (see <see cref="OutboxConsumerName"/>).
    /// Rows written by older builds hold the short type name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
