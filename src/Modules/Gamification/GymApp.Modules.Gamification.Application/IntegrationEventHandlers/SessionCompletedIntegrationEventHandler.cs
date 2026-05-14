using GymApp.Modules.Scheduling.IntegrationEvents;
using MediatR;

namespace GymApp.Modules.Gamification.Application.IntegrationEventHandlers;

internal sealed class SessionCompletedIntegrationEventHandler
    : INotificationHandler<SessionCompletedIntegrationEvent>
{
    // This handler tracks eligible sessions for rating prompts.
    // When a session is completed, the member becomes eligible to rate the trainer.
    // The actual rating submission happens through SubmitTrainerRatingCommand.
    // Future enhancement: store eligible session records for rating prompt logic.

    public Task Handle(SessionCompletedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        // Placeholder: In a full implementation, this would store the session as
        // eligible for rating, enabling the client app to prompt the member.
        // For now, the rating flow is initiated by the client app after session completion.
        return Task.CompletedTask;
    }
}
