using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Domain.ValueObjects;

namespace GymApp.Modules.Scheduling.Domain.Entities;

public sealed class MemberLog : Entity
{
    private MemberLog() : base() { }

    private MemberLog(Guid id) : base(id) { }

    public Guid SessionId { get; private set; }
    public int Rating { get; private set; }
    public int PerceivedExertion { get; private set; }
    public string? PersonalNotes { get; private set; }
    public string? Discomfort { get; private set; }
    public int Satisfaction { get; private set; }
    public DateTime LoggedAt { get; private set; }

    public static MemberLog Create(Guid sessionId, MemberLogDetails details)
    {
        if (details.Rating < 1 || details.Rating > 5)
            throw new ArgumentOutOfRangeException(nameof(details), "Rating must be between 1 and 5.");

        if (details.PerceivedExertion < 1 || details.PerceivedExertion > 10)
            throw new ArgumentOutOfRangeException(nameof(details), "Perceived exertion (RPE) must be between 1 and 10.");

        if (details.Satisfaction < 1 || details.Satisfaction > 5)
            throw new ArgumentOutOfRangeException(nameof(details), "Satisfaction must be between 1 and 5.");

        return new MemberLog(Guid.NewGuid())
        {
            SessionId = sessionId,
            Rating = details.Rating,
            PerceivedExertion = details.PerceivedExertion,
            PersonalNotes = details.PersonalNotes,
            Discomfort = details.Discomfort,
            Satisfaction = details.Satisfaction,
            LoggedAt = DateTime.UtcNow
        };
    }
}
