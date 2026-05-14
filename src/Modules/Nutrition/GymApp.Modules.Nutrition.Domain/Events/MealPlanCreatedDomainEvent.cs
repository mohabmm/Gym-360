using GymApp.Common.Domain;

namespace GymApp.Modules.Nutrition.Domain.Events;

public sealed record MealPlanCreatedDomainEvent(Guid MealPlanId, Guid TrainerId) : IDomainEvent;
