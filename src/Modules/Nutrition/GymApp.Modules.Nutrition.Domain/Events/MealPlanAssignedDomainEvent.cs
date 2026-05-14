using GymApp.Common.Domain;

namespace GymApp.Modules.Nutrition.Domain.Events;

public sealed record MealPlanAssignedDomainEvent(Guid MealPlanId, Guid TrainerId, Guid ClientId) : IDomainEvent;
