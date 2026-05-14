using GymApp.Modules.Identity.IntegrationEvents;
using GymApp.Modules.Training.Application.Abstractions;
using GymApp.Modules.Training.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Training.Application.IntegrationEvents;

internal sealed class AccountDeletionRequestedIntegrationEventHandler
    : INotificationHandler<AccountDeletionRequestedIntegrationEvent>
{
    private readonly IWorkoutLogRepository _workoutLogRepository;
    private readonly ITrainingUnitOfWork _unitOfWork;
    private readonly ILogger<AccountDeletionRequestedIntegrationEventHandler> _logger;

    public AccountDeletionRequestedIntegrationEventHandler(
        IWorkoutLogRepository workoutLogRepository,
        ITrainingUnitOfWork unitOfWork,
        ILogger<AccountDeletionRequestedIntegrationEventHandler> logger)
    {
        _workoutLogRepository = workoutLogRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(AccountDeletionRequestedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing account deletion for user {UserId} in Training module. Scheduled deletion: {ScheduledAt}",
            notification.UserId, notification.ScheduledDeletionUtc);

        // Retrieve all workout logs for the user over their entire history (up to 10 years)
        var workoutLogs = await _workoutLogRepository.GetByUserAsync(
            notification.UserId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            cancellationToken);

        // Workout assignments are gym-scoped and will be handled by the gym-level cleanup job.
        // Workout logs reference userId — they will remain for audit/anonymization on the scheduled deletion date.
        // For now, log the count so the purge job knows what to clean up.
        _logger.LogInformation(
            "User {UserId} has {Count} workout logs scheduled for anonymization on {Date}",
            notification.UserId, workoutLogs.Count, notification.ScheduledDeletionUtc);

        // Note: actual data purge happens in the scheduled background job at ScheduledDeletionUtc.
        // This handler marks the intent; the job will nullify UserId (anonymize) or delete rows.
        await Task.CompletedTask;
    }
}
