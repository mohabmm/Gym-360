using GymApp.Common.Application.Messaging;
using GymApp.Modules.Billing.Application.DTOs;

namespace GymApp.Modules.Billing.Application.Queries.GetTrainerEarnings;

public sealed record GetTrainerEarningsQuery(
    Guid GymId,
    Guid TrainerId,
    DateOnly? From,
    DateOnly? To) : IQuery<TrainerEarningsSummaryDto>;
