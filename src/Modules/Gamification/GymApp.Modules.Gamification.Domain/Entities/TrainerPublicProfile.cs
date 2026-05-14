using GymApp.Common.Domain;

namespace GymApp.Modules.Gamification.Domain.Entities;

public sealed class TrainerPublicProfile : Entity
{
    private readonly List<Testimonial> _testimonials = new();

    private TrainerPublicProfile() { }

    private TrainerPublicProfile(
        Guid id,
        Guid trainerId,
        Guid gymId)
        : base(id)
    {
        TrainerId = trainerId;
        GymId = gymId;
        DisplayRating = 0m;
        CoachOfTheMonthCount = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid TrainerId { get; private set; }
    public Guid GymId { get; private set; }
    public decimal DisplayRating { get; private set; }
    public IReadOnlyList<Testimonial> Testimonials => _testimonials.AsReadOnly();
    public int CoachOfTheMonthCount { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static Result<TrainerPublicProfile> Create(
        Guid trainerId,
        Guid gymId)
    {
        var profile = new TrainerPublicProfile(
            Guid.NewGuid(),
            trainerId,
            gymId);

        return Result.Success(profile);
    }

    public Testimonial AddTestimonial(string comment, bool approved)
    {
        var testimonial = Testimonial.Create(Id, comment, approved);
        _testimonials.Add(testimonial);
        UpdatedAt = DateTime.UtcNow;
        return testimonial;
    }

    public void UpdateDisplayRating(decimal newRating)
    {
        DisplayRating = newRating;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementCoachOfTheMonthCount()
    {
        CoachOfTheMonthCount++;
        UpdatedAt = DateTime.UtcNow;
    }
}
