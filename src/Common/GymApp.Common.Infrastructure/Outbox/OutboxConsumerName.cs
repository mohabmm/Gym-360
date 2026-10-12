namespace GymApp.Common.Infrastructure.Outbox;

/// <summary>
/// Identity of an integration-event handler in the <see cref="OutboxMessageConsumer"/> ledger.
/// </summary>
public static class OutboxConsumerName
{
    // Matches the outbox_message_consumers.Name column (part of the clustered PK).
    private const int MaxLength = 200;

    /// <summary>
    /// Ledger name for a handler type: its namespace-qualified name. The short type name
    /// is NOT unique — several modules each declare e.g. a
    /// <c>SessionCompletedIntegrationEventHandler</c> — and keying the ledger on it made
    /// the first handler to run mark the message consumed for all of its namesakes, so the
    /// others (Billing's package deduction among them) were silently never invoked.
    /// </summary>
    public static string For(Type handlerType)
    {
        var name = handlerType.FullName ?? handlerType.Name;
        // Keep the distinguishing tail (type name + nearest namespaces) if ever too long.
        return name.Length <= MaxLength ? name : name[^MaxLength..];
    }

    /// <summary>
    /// Whether <paramref name="handlerType"/> already consumed a message, given the ledger
    /// names recorded for that message and every handler type subscribed to the event.
    /// Rows written before the ledger switched to qualified names hold the short type name;
    /// such a row is honoured only when that short name is unambiguous among the event's
    /// handlers — otherwise it cannot say which namesake actually ran.
    /// </summary>
    public static bool IsConsumed(
        Type handlerType,
        IReadOnlyCollection<string> recordedNames,
        IReadOnlyCollection<Type> allHandlerTypes)
    {
        if (recordedNames.Contains(For(handlerType)))
            return true;

        var shortName = handlerType.Name;
        return recordedNames.Contains(shortName)
            && allHandlerTypes.Count(t => t.Name == shortName) == 1;
    }
}
