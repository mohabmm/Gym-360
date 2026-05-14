using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gamification.Application.Commands.ModerateTestimonial;

public sealed record ModerateTestimonialCommand(
    Guid TestimonialId,
    Guid TrainerId,
    Guid GymId,
    bool IsApproved,
    Guid ModeratedBy) : ICommand;
