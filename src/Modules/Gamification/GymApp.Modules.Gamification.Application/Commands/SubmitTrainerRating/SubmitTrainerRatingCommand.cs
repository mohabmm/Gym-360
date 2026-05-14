using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gamification.Application.Commands.SubmitTrainerRating;

public sealed record SubmitTrainerRatingCommand(
    Guid GymId,
    Guid TrainerId,
    Guid ClientId,
    Guid SessionId,
    int Stars,
    string? Comment) : ICommand<Guid>;
