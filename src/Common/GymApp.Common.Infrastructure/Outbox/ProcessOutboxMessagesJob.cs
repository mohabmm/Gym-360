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
            .Select(c => (c.OutboxMessageId, c.Name))
            .ToHashSet();

        foreach (var message in messages)
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
                continue;
            }

            if (integrationEvent is null)
            {
                _logger.LogWarning("Null payload for outbox message {MessageId}", message.Id);
                message.Error = "Deserialized payload was null.";
                message.ProcessedOnUtc = DateTime.UtcNow;
                continue;
            }

            var eventType = integrationEvent.GetType();
            var handlerType = typeof(INotificationHandler<>).MakeGenericType(eventType);
            var handleMethod = handlerType.GetMethod("Handle")!;
            var handlers = sp.GetServices(handlerType);

            var failed = false;
            Exception? lastError = null;

            foreach (var handler in handlers)
            {
                if (handler is null) continue;
                var handlerName = handler.GetType().Name;

                if (consumed.Contains((message.Id, handlerName)))
                    continue; // already ran on a previous attempt

                try
                {
                    await (Task)handleMethod.Invoke(handler, new[] { integrationEvent, cancellationToken })!;

                    // Record consumption immediately so a crash/failure after this handler does not
                    // re-run it on the next tick (shrinks the at-least-once double-fire window).
                    dbContext.OutboxMessageConsumers.Add(new OutboxMessageConsumer
                    {
                        OutboxMessageId = message.Id,
                        Name = handlerName
                    });
                    await dbContext.SaveChangesAsync(cancellationToken);
                    consumed.Add((message.Id, handlerName));
                }
                catch (Exception ex)
                {
                    // Keep going so independent handlers still run; this handler retries next tick.
                    failed = true;
                    lastError = ex;
                    _logger.LogError(ex,
                        "Handler {Handler} failed for outbox message {MessageId} (type {Type}, gym {Prefix})",
                        handlerName, message.Id, message.Type, gymRegistration?.SchemaPrefix ?? "soft");
                }
            }

            if (!failed)
            {
                message.ProcessedOnUtc = DateTime.UtcNow;
            }
            else
            {
                message.RetryCount++;
                message.Error = lastError?.ToString();
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

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
