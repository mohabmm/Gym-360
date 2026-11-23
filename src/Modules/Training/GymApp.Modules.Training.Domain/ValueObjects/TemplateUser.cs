namespace GymApp.Modules.Training.Domain.ValueObjects;

/// <summary>Someone working with plan templates in a gym: a coach, or the gym's owner/manager.</summary>
public sealed record TemplateUser(Guid GymId, Guid UserId, bool IsGymManager = false);
