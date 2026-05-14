namespace GymApp.Modules.Scheduling.Application.DTOs;

public sealed record MemberLogDto(
    Guid Id,
    int Rating,
    int PerceivedExertion,
    string? PersonalNotes,
    string? Discomfort,
    int Satisfaction,
    DateTime LoggedAt);
