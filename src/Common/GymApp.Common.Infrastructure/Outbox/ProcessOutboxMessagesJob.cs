using GymApp.Common.Application.Tenancy;
using GymApp.Common.Infrastructure.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;

namespace GymApp.Common.Infrastructure.Outbox;

[DisallowConcurrentExecution]
public sealed class ProcessOutboxMessagesJob : IJob
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        TypeNameHandling = TypeNameHandling.All
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TenancyOptions _tenancyOptions;
    private readonly OutboxOptions _options;
    private readonly ILogger<ProcessOutboxMessagesJob> _logger;

    public ProcessOutboxMessagesJob(
        IServiceScopeFactory scopeFactory,
        IOptions<TenancyOptions> tenancyOptions,
        IOptions<OutboxOptions> options,
        ILogger<ProcessOutboxMessagesJob> logger)
    {
        _scopeFactory = scopeFactory;
        _tenancyOptions = tenancyOptions.Value;
        _options = options.Value;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        if (_tenancyOptions.Mode == TenancyMode.Hard)
        {
            using var scope = _scopeFactory.CreateScope();
            var registry = scope.ServiceProvider.GetRequiredService<IGymRegistry>();
            var gyms = await registry.GetActiveAsync(context.CancellationToken);

            foreach (var gym in gyms)
                await ProcessGymAsync(gym, context.CancellationToken);
        }
        else
        {
            // Soft mode: drain the legacy central outbox once per tick.
            await ProcessGymAsync(gymRegistration: null, context.CancellationToken);
        }
    }

    private async Task ProcessGymAsync(GymRegistration? gymRegistration, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        if (gymRegistration is not null)
        {
            var accessor = sp.GetRequiredService<ITenantContextAccessor>();
            accessor.Set(gymRegistration.GymId, gymRegistration.SchemaPrefix, isOwnerScope: false);
        }

        var dbContext = sp.GetRequiredService<OutboxDbContext>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0) return;

        // Pre-load which (message, handler) pairs already ran, so a retried message only
        // re-invokes the handler(s) that previously failed.
        var messageIds = messages.Select(m => m.Id).ToList();
        var consumed = (await dbContext.OutboxMessageConsumers
                .Where(c => messageIds.Contains(c.OutboxMessageId))
                .ToListAsync(cancellationToken))
            .GroupBy(c => c.OutboxMessageId)
            .ToDictionary(g => g.Key, g => g.Select(c => c.Name).ToHashSet());

        var batch = new Batch(sp, dbContext, gymRegistration?.SchemaPrefix ?? "soft", cancellationToken);
        foreach (var message in messages)
        {
            var consumedNames = consumed.GetValueOrDefault(message.Id) ?? new HashSet<string>();
            await ProcessMessageAsync(batch, message, consumedNames);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record Batch(
        IServiceProvider Services,
        OutboxDbContext DbContext,
        string GymLabel,
        CancellationToken CancellationToken);

    private async Task ProcessMessageAsync(Batch batch, OutboxMessage message, HashSet<string> consumedNames)
    {
        var integrationEvent = TryDeserialize(message);
        if (integrationEvent is null)
            return;

        var lastError = await RunHandlersAsync(batch, message, integrationEvent, consumedNames);
        ApplyOutcome(message, lastError);
    }

    /// <summary>
    /// Returns the deserialized event, or null after dead-lettering a message whose
    /// content can never be processed.
    /// </summary>
    private object? TryDeserialize(OutboxMessage message)
    {
        object? integrationEvent;
        try
        {
            integrationEvent = JsonConvert.DeserializeObject(message.Content, JsonSettings);
        }
        catch (Exception ex)
        {
            // Undeserializable content never succeeds — dead-letter immediately.
            _logger.LogError(ex, "Unable to deserialize outbox message {MessageId}", message.Id);
            message.Error = ex.ToString();
            message.ProcessedOnUtc = DateTime.UtcNow;
            return null;
        }

        if (integrationEvent is null)
        {
            _logger.LogWarning("Null payload for outbox message {MessageId}", message.Id);
            message.Error = "Deserialized payload was null.";
            message.ProcessedOnUtc = DateTime.UtcNow;
        }

        return integrationEvent;
    }

    /// <summary>
    /// Invokes every not-yet-consumed handler for the event. Returns the last handler
    /// failure, or null when all handlers succeeded.
    /// </summary>
    private async Task<Exception?> RunHandlersAsync(
        Batch batch, OutboxMessage message, object integrationEvent, HashSet<string> consumedNames)
    {
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(integrationEvent.GetType());
        var handleMethod = handlerType.GetMethod("Handle")!;
        var handlers = batch.Services.GetServices(handlerType).Where(h => h is not null).Select(h => h!).ToList();
        var handlerTypes = handlers.Select(h => h.GetType()).ToList();

        Exception? lastError = null;

        foreach (var handler in handlers)
        {
            // Namespace-qualified: several modules declare handlers with the same short
            // type name for one event, and each of them must run.
            var handlerName = OutboxConsumerName.For(handler.GetType());

            if (OutboxConsumerName.IsConsumed(handler.GetType(), consumedNames, handlerTypes))
                continue; // already ran on a previous attempt

            try
            {
                await (Task)handleMethod.Invoke(handler, new[] { integrationEvent, batch.CancellationToken })!;

                // Record consumption immediately so a crash/failure after this handler does not
                // re-run it on the next tick (shrinks the at-least-once double-fire window).
                batch.DbContext.OutboxMessageConsumers.Add(new OutboxMessageConsumer
                {
                    OutboxMessageId = message.Id,
                    Name = handlerName
                });
                await batch.DbContext.SaveChangesAsync(batch.CancellationToken);
                consumedNames.Add(handlerName);
            }
            catch (Exception ex)
            {
                // Keep going so independent handlers still run; this handler retries next tick.
                lastError = ex;
                _logger.LogError(ex,
                    "Handler {Handler} failed for outbox message {MessageId} (type {Type}, gym {Prefix})",
                    handlerName, message.Id, message.Type, batch.GymLabel);
            }
        }

        return lastError;
    }

    private void ApplyOutcome(OutboxMessage message, Exception? lastError)
    {
        if (lastError is null)
        {
            message.ProcessedOnUtc = DateTime.UtcNow;
            return;
        }

        message.RetryCount++;
        message.Error = lastError.ToString();
        if (message.RetryCount >= _options.MaxRetryCount)
        {
            // Dead-letter: stop retrying so a poison message cannot wedge the queue.
            message.ProcessedOnUtc = DateTime.UtcNow;
            _logger.LogError(
                "Outbox message {MessageId} (type {Type}) dead-lettered after {Attempts} attempts",
                message.Id, message.Type, message.RetryCount);
        }
    }
}
