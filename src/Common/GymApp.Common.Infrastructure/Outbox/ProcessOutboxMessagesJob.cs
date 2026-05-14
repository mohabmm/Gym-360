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

        var publisher = sp.GetRequiredService<IPublisher>();
        var dbContext = sp.GetRequiredService<OutboxDbContext>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0) return;

        foreach (var message in messages)
        {
            try
            {
                var domainEvent = JsonConvert.DeserializeObject(message.Content, JsonSettings);
                if (domainEvent is null)
                {
                    _logger.LogWarning("Unable to deserialize outbox message {MessageId}", message.Id);
                    continue;
                }

                await publisher.Publish(domainEvent, cancellationToken);
                message.ProcessedOnUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox message {MessageId} (gym {Prefix})",
                    message.Id, gymRegistration?.SchemaPrefix ?? "soft");
                message.Error = ex.ToString();
                message.ProcessedOnUtc = DateTime.UtcNow;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
