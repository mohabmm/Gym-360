using GymApp.Common.Domain;
using GymApp.Modules.Gamification.Domain.DomainEvents;
using GymApp.Modules.Gamification.Domain.Errors;
using GymApp.Modules.Gamification.Domain.ValueObjects;

namespace GymApp.Modules.Gamification.Domain.Entities;

public sealed class TrainerRating : Entity
{
    private TrainerRating() { }

    private TrainerRating(Guid id) : base(id) { }

    public Guid GymId { get; private set; }
    public Guid TrainerId { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid SessionId { get; private set; }
    public int Stars { get; private set; }
    public string? Comment { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Result<TrainerRating> Create(TrainerRatingDetails details)
    {
        if (details.Stars < 1 || details.Stars > 5)
        {
            return Result.Failure<TrainerRating>(GamificationErrors.InvalidStarRating);
        }

        var rating = new TrainerRating(Guid.NewGuid())
        {
            GymId = details.GymId,
            TrainerId = details.TrainerId,
            ClientId = details.ClientId,
            SessionId = details.SessionId,
            Stars = details.Stars,
            Comment = details.Comment,
            CreatedAt = DateTime.UtcNow
        };

        rating.RaiseDomainEvent(new TrainerRatedDomainEvent(
            rating.Id,
            details.TrainerId,
            details.GymId,
            details.Stars));

        return Result.Success(rating);
    }
}
