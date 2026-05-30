using GymApp.Common.Domain;

namespace GymApp.Modules.Gamification.IntegrationEvents;

public sealed record CoachOfTheMonthSelectedIntegrationEvent(
    Guid TrainerId,
    Guid GymId,
    int Month,
    int Year) : IIntegrationEvent;
