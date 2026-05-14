using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gamification.Application.DTOs;

namespace GymApp.Modules.Gamification.Application.Queries.GetTrainerRatings;

public sealed record GetTrainerRatingsQuery(
    Guid GymId,
    Guid TrainerId,
    DateOnly? From,
    DateOnly? To) : IQuery<List<TrainerRatingDto>>;
