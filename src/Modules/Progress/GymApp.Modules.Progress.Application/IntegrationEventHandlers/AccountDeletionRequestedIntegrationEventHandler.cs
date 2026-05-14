using GymApp.Modules.Identity.IntegrationEvents;
using GymApp.Modules.Progress.Application.Abstractions;
using GymApp.Modules.Progress.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Progress.Application.IntegrationEventHandlers;

internal sealed class AccountDeletionRequestedIntegrationEventHandler
    : INotificationHandler<AccountDeletionRequestedIntegrationEvent>
{
    private readonly IProgressPhotoRepository _photoRepository;
    private readonly IStreakRepository _streakRepository;
    private readonly IProgressUnitOfWork _unitOfWork;
    private readonly ILogger<AccountDeletionRequestedIntegrationEventHandler> _logger;

    public AccountDeletionRequestedIntegrationEventHandler(
        IProgressPhotoRepository photoRepository,
        IStreakRepository streakRepository,
        IProgressUnitOfWork unitOfWork,
        ILogger<AccountDeletionRequestedIntegrationEventHandler> logger)
    {
        _photoRepository = photoRepository;
        _streakRepository = streakRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Task Handle(AccountDeletionRequestedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        // Progress data (body metrics, photos, streaks) is gym-scoped.
        // The scheduled purge job will handle deletion of all records for this userId across all gyms.
        // Log the intent so the purge job processes it.
        _logger.LogInformation(
            "Account deletion requested for user {UserId} in Progress module. " +
            "Progress photos, body metrics, and streaks will be deleted on {ScheduledAt}",
            notification.UserId, notification.ScheduledDeletionUtc);

        return Task.CompletedTask;
    }
}
