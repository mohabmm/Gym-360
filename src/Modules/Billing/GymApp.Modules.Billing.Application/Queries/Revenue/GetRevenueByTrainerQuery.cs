using GymApp.Common.Application.Messaging;
using GymApp.Modules.Billing.Application.DTOs;

namespace GymApp.Modules.Billing.Application.Queries.Revenue;

public sealed record GetRevenueByTrainerQuery(
    Guid GymId,
    DateOnly From,
    DateOnly To) : IQuery<List<TrainerRevenueDto>>;
