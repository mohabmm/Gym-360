namespace GymApp.Modules.Billing.Application.DTOs;

public sealed record TrainerRevenueDto(
    Guid TrainerId,
    int SessionCount,
    decimal TotalRevenue,
    string Currency);
