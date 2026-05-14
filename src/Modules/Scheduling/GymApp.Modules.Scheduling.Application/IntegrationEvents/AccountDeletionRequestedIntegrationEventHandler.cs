using GymApp.Modules.Identity.IntegrationEvents;
using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GymApp.Modules.Scheduling.Application.IntegrationEvents;

internal sealed class AccountDeletionRequestedIntegrationEventHandler
    : INotificationHandler<AccountDeletionRequestedIntegrationEvent>
{
    private readonly ISessionRepository _sessionRepository;
    private readonly ISchedulingUnitOfWork _unitOfWork;
    private readonly ILogger<AccountDeletionRequestedIntegrationEventHandler> _logger;

    public AccountDeletionRequestedIntegrationEventHandler(
        ISessionRepository sessionRepository,
        ISchedulingUnitOfWork unitOfWork,
        ILogger<AccountDeletionRequestedIntegrationEventHandler> logger)
    {
        _sessionRepository = sessionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(AccountDeletionRequestedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing account deletion for user {UserId} in Scheduling module",
            notification.UserId);

        // Cancel all upcoming sessions where user is a member
        var upcomingSessions = await _sessionRepository.GetUpcomingByMemberAsync(
            notification.UserId, cancellationToken);

        var cancelledCount = 0;
        foreach (var session in upcomingSessions)
        {
            if (session.Status is SessionStatus.Pending or SessionStatus.Confirmed)
            {
                var result = session.Cancel(
                    reason: "Account deletion requested",
                    cancelledBy: notification.UserId,
                    isLateCancellation: false);

                if (result.IsSuccess)
                {
                    _sessionRepository.Update(session);
                    cancelledCount++;
                }
            }
        }

        if (cancelledCount > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Cancelled {Count} upcoming sessions for user {UserId}",
            cancelledCount, notification.UserId);
    }
}
