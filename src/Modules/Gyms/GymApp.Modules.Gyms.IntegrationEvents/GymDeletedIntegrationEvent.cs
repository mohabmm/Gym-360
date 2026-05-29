using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.IntegrationEvents;

public sealed record GymDeletedIntegrationEvent(Guid GymId) : IIntegrationEvent;
