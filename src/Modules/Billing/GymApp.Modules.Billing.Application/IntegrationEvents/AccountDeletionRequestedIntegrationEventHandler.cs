using GymApp.Modules.Identity.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Billing.Application.IntegrationEvents;

internal sealed class AccountDeletionRequestedIntegrationEventHandler
    : INotificationHandler<AccountDeletionRequestedIntegrationEvent>
{
    private readonly ILogger<AccountDeletionRequestedIntegrationEventHandler> _logger;

    public AccountDeletionRequestedIntegrationEventHandler(
        ILogger<AccountDeletionRequestedIntegrationEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(AccountDeletionRequestedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        // Billing data (payments, membership plans, session packages) is gym-scoped.
        // Per Egypt PDPL, payment records must be retained for 7 years for financial compliance.
        // On scheduled deletion, member personal data (name, contact) will be anonymized
        // while payment records are retained with a tombstone reference.
        _logger.LogInformation(
            "Account deletion requested for user {UserId} in Billing module. " +
            "Payment records will be anonymized on {ScheduledAt} per PDPL retention rules.",
            notification.UserId, notification.ScheduledDeletionUtc);

        // Actual anonymization of payment MemberId references occurs in the scheduled purge job.
        return Task.CompletedTask;
    }
}
