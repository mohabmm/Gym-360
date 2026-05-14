using GymApp.Modules.Gyms.IntegrationEvents;
using GymApp.Modules.Progress.Application.Commands.UpdateStreak;
using MediatR;

namespace GymApp.Modules.Progress.Application.IntegrationEventHandlers;

internal sealed class CheckInRecordedIntegrationEventHandler : INotificationHandler<CheckInRecordedIntegrationEvent>
{
    private readonly ISender _sender;

    public CheckInRecordedIntegrationEventHandler(ISender sender)
    {
        _sender = sender;
    }

    public async Task Handle(CheckInRecordedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var command = new UpdateStreakCommand(
            notification.MemberId,
            notification.GymId,
            "DailyCheckIn",
            DateOnly.FromDateTime(notification.CheckedInAt));

        await _sender.Send(command, cancellationToken);
    }
}
