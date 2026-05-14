namespace GymApp.Modules.Gamification.Application.DTOs;

public sealed record PendingTestimonialDto(
    Guid Id,
    Guid TrainerId,
    Guid GymId,
    string CommentText,
    DateTime CreatedAt);
