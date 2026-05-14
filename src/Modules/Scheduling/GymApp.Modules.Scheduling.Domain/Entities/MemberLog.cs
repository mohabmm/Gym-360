using GymApp.Common.Domain;

namespace GymApp.Modules.Scheduling.Domain.Entities;

public sealed class MemberLog : Entity
{
    private MemberLog() : base() { }

    private MemberLog(
        Guid id,
        Guid sessionId,
        int rating,
        int perceivedExertion,
        string? personalNotes,
        string? discomfort,
        int satisfaction) : base(id)
    {
        SessionId = sessionId;
        Rating = rating;
        PerceivedExertion = perceivedExertion;
        PersonalNotes = personalNotes;
        Discomfort = discomfort;
        Satisfaction = satisfaction;
        LoggedAt = DateTime.UtcNow;
    }

    public Guid SessionId { get; private set; }
    public int Rating { get; private set; }
    public int PerceivedExertion { get; private set; }
    public string? PersonalNotes { get; private set; }
    public string? Discomfort { get; private set; }
    public int Satisfaction { get; private set; }
    public DateTime LoggedAt { get; private set; }

    public static MemberLog Create(
        Guid sessionId,
        int rating,
        int perceivedExertion,
        string? personalNotes,
        string? discomfort,
        int satisfaction)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");

        if (perceivedExertion < 1 || perceivedExertion > 10)
            throw new ArgumentOutOfRangeException(nameof(perceivedExertion), "Perceived exertion (RPE) must be between 1 and 10.");

        if (satisfaction < 1 || satisfaction > 5)
            throw new ArgumentOutOfRangeException(nameof(satisfaction), "Satisfaction must be between 1 and 5.");

        return new MemberLog(
            Guid.NewGuid(),
            sessionId,
            rating,
            perceivedExertion,
            personalNotes,
            discomfort,
            satisfaction);
    }
}
