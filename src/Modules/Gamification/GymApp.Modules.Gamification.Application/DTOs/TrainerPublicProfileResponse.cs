namespace GymApp.Modules.Gamification.Application.DTOs;

public sealed record TrainerPublicProfileResponse(
    Guid TrainerId,
    Guid GymId,
    decimal DisplayRating,
    int CoachOfTheMonthCount,
    List<TestimonialDto> Testimonials);

public sealed record TestimonialDto(
    Guid Id,
    string CommentText,
    bool IsApproved,
    DateTime CreatedAt);
