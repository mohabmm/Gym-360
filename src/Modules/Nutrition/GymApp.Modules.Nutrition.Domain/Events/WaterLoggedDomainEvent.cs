using GymApp.Common.Domain;

namespace GymApp.Modules.Nutrition.Domain.Events;

public sealed record WaterLoggedDomainEvent(Guid WaterLogId, Guid UserId) : IDomainEvent;
