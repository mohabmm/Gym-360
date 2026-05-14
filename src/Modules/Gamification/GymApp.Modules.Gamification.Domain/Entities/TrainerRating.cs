using GymApp.Common.Domain;
using GymApp.Modules.Gamification.Domain.DomainEvents;
using GymApp.Modules.Gamification.Domain.Errors;

namespace GymApp.Modules.Gamification.Domain.Entities;

public sealed class TrainerRating : Entity
{
    private TrainerRating() { }

    private TrainerRating(
        Guid id,
        Guid gymId,
        Guid trainerId,
        Guid clientId,
        Guid sessionId,
        int stars,
        string? comment)
        : base(id)
    {
        GymId = gymId;
        TrainerId = trainerId;
        ClientId = clientId;
        SessionId = sessionId;
        Stars = stars;
        Comment = comment;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid GymId { get; private set; }
    public Guid TrainerId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid SessionId { get; private set; }
    public int Stars { get; private set; }
    public string? Comment { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Result<TrainerRating> Create(
        Guid gymId,
        Guid trainerId,
        Guid clientId,
        Guid sessionId,
        int stars,
        string? comment)
    {
        if (stars < 1 || stars > 5)
        {
            return Result.Failure<TrainerRating>(GamificationErrors.InvalidStarRating);
        }

        var rating = new TrainerRating(
            Guid.NewGuid(),
            gymId,
            trainerId,
            clientId,
            sessionId,
            stars,
            comment);

        rating.RaiseDomainEvent(new TrainerRatedDomainEvent(
            rating.Id,
            trainerId,
            gymId,
            stars));

        return Result.Success(rating);
    }
}
