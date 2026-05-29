namespace GymApp.Common.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public int IntervalInSeconds { get; init; } = 10;
    public int BatchSize { get; init; } = 20;

    /// <summary>Max delivery attempts before a message is dead-lettered (marked processed with its
    /// last error preserved). Prevents a permanently-failing message from blocking the queue.</summary>
    public int MaxRetryCount { get; init; } = 5;
}
