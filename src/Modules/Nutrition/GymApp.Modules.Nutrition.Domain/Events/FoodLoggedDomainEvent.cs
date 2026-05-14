using GymApp.Common.Domain;

namespace GymApp.Modules.Nutrition.Domain.Events;

public sealed record FoodLoggedDomainEvent(Guid FoodLogId, Guid UserId) : IDomainEvent;
