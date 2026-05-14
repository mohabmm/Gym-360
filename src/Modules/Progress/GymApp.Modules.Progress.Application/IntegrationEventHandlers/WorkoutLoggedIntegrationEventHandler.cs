using GymApp.Modules.Progress.Application.Commands.UpdateStreak;
using GymApp.Modules.Training.IntegrationEvents;
using MediatR;

namespace GymApp.Modules.Progress.Application.IntegrationEventHandlers;

internal sealed class WorkoutLoggedIntegrationEventHandler : INotificationHandler<WorkoutLoggedIntegrationEvent>
{
    private readonly ISender _sender;

    public WorkoutLoggedIntegrationEventHandler(ISender sender)
    {
        _sender = sender;
    }

    public async Task Handle(WorkoutLoggedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var command = new UpdateStreakCommand(
            notification.UserId,
            notification.GymId,
            "DailyWorkout",
            DateOnly.FromDateTime(notification.LoggedAt));

        await _sender.Send(command, cancellationToken);
    }
}
