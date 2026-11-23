using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Enums;
using GymApp.Modules.Training.Domain.ValueObjects;

namespace GymApp.Modules.Training.Domain.Entities;

/// <summary>
/// pt.training_plan: a reusable plan. Gym templates are made by the owner or a manager and every
/// coach of the gym can use them; coach templates belong to one coach (founder decision Q4: they
/// disappear from the gym with the coach). Assigning a template copies it: a trainee's plan never
/// points back at the template's content.
/// </summary>
public sealed class PlanTemplate : Entity
{
    private PlanTemplate() { }

    private PlanTemplate(Guid id) : base(id) { }

    public Guid GymId { get; private set; }
    public PlanTemplateScope Scope { get; private set; }
    /// <summary>Set for coach templates only.</summary>
    public Guid? OwnerTrainerId { get; private set; }
    public Guid CreatedBy { get; private set; }
    public string Name { get; private set; } = default!;
    public string? NameAr { get; private set; }
    public PlanGoal Goal { get; private set; }
    public string? GoalText { get; private set; }
    public int DurationWeeks { get; private set; }
    public int DaysPerWeek { get; private set; }
    public PlanDocument Document { get; private set; } = default!;
    /// <summary>Seeded sample; editable like any other gym template.</summary>
    public bool IsSample => SampleKey is not null;
    /// <summary>Which seeded sample this is (unique per gym, so samples are seeded once).</summary>
    public string? SampleKey { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public PlanContent Content => new(Name, NameAr, Goal, GoalText, Document);

    public static PlanTemplate CreateForGym(Guid gymId, Guid createdBy, PlanContent content) =>
        Create(gymId, PlanTemplateScope.Gym, ownerTrainerId: null, createdBy, content, sampleKey: null);

    public static PlanTemplate CreateForCoach(Guid gymId, Guid trainerId, PlanContent content) =>
        Create(gymId, PlanTemplateScope.Coach, trainerId, trainerId, content, sampleKey: null);

    /// <summary>A gym template seeded by the system (<paramref name="sampleKey"/> is unique per gym).</summary>
    public static PlanTemplate CreateSample(Guid gymId, string sampleKey, PlanContent content) =>
        Create(gymId, PlanTemplateScope.Gym, ownerTrainerId: null, Guid.Empty, content, sampleKey);

    private static PlanTemplate Create(
        Guid gymId, PlanTemplateScope scope, Guid? ownerTrainerId, Guid createdBy, PlanContent content, string? sampleKey)
    {
        var now = DateTime.UtcNow;
        var template = new PlanTemplate(Guid.NewGuid())
        {
            GymId = gymId,
            Scope = scope,
            OwnerTrainerId = ownerTrainerId,
            CreatedBy = createdBy,
            SampleKey = sampleKey,
            CreatedAt = now
        };
        template.Apply(content, now);
        return template;
    }

    /// <summary>Coaches see gym templates and their own.</summary>
    public bool IsVisibleTo(Guid userId) => Scope == PlanTemplateScope.Gym || OwnerTrainerId == userId;

    /// <summary>Gym templates: owner or manager. Coach templates: their coach.</summary>
    public bool CanBeEditedBy(Guid userId, bool isGymManager) =>
        Scope == PlanTemplateScope.Gym ? isGymManager : OwnerTrainerId == userId;

    public void Update(PlanContent content) => Apply(content, DateTime.UtcNow);

    public void Archive()
    {
        IsArchived = true;
        UpdatedAt = DateTime.UtcNow;
    }

    private void Apply(PlanContent content, DateTime now)
    {
        Name = content.Name;
        NameAr = content.NameAr;
        Goal = content.Goal;
        GoalText = content.GoalText;
        Document = content.Document;
        DurationWeeks = content.DurationWeeks;
        DaysPerWeek = content.DaysPerWeek;
        UpdatedAt = now;
    }
}
