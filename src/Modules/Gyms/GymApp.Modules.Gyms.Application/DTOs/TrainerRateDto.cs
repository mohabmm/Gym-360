namespace GymApp.Modules.Gyms.Application.DTOs;

public sealed record TrainerRateDto(
    Guid GymId,
    Guid TrainerId,
    decimal RatePerSession,
    string Currency,
    DateTime EffectiveFrom);
