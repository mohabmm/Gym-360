namespace GymApp.Common.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public DateTime OccurredOnUtc { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }

    /// <summary>
    /// Number of failed delivery attempts. Incremented (and <see cref="ProcessedOnUtc"/> left null
    /// for retry) when a handler throws; once it reaches OutboxOptions.MaxRetryCount the message is
    /// dead-lettered (ProcessedOnUtc stamped, Error kept) so a poison message cannot wedge the queue.
    /// </summary>
    public int RetryCount { get; set; }
}
