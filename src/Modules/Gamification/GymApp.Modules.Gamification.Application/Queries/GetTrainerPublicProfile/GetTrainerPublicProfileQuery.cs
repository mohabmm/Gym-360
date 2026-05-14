using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gamification.Application.DTOs;

namespace GymApp.Modules.Gamification.Application.Queries.GetTrainerPublicProfile;

public sealed record GetTrainerPublicProfileQuery(
    Guid TrainerId,
    Guid GymId) : IQuery<TrainerPublicProfileResponse>;
