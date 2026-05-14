using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Domain.Errors;
using GymApp.Modules.Nutrition.Domain.Events;
using GymApp.Modules.Nutrition.Domain.ValueObjects;

namespace GymApp.Modules.Nutrition.Domain.Entities;

public sealed class MealPlan : Entity
{
    private readonly List<MealPlanDay> _days = new();

    private MealPlan() { }

    public Guid TrainerId { get; private set; }
    public Guid? ClientId { get; private set; }
    public Guid GymId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal DailyCalorieTarget { get; private set; }
    public MacroTargets MacroTargets { get; private set; } = default!;
    public bool IsTemplate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<MealPlanDay> Days => _days.AsReadOnly();

    public static MealPlan Create(
        Guid trainerId,
        Guid gymId,
        string name,
        string? description,
        decimal dailyCalorieTarget,
        MacroTargets macroTargets,
        bool isTemplate)
    {
        var plan = new MealPlan
        {
            Id = Guid.NewGuid(),
            TrainerId = trainerId,
            ClientId = null,
            GymId = gymId,
            Name = name,
            Description = description,
            DailyCalorieTarget = dailyCalorieTarget,
            MacroTargets = macroTargets,
            IsTemplate = isTemplate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        plan.RaiseDomainEvent(new MealPlanCreatedDomainEvent(plan.Id, trainerId));

        return plan;
    }

    public Result AssignToClient(Guid clientId)
    {
        if (ClientId is not null)
            return Result.Failure(NutritionErrors.MealPlanAlreadyAssigned);

        ClientId = clientId;
        IsTemplate = false;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new MealPlanAssignedDomainEvent(Id, TrainerId, clientId));

        return Result.Success();
    }

    public void AddDay(MealPlanDay day)
    {
        _days.Add(day);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(
        string name,
        string? description,
        decimal dailyCalorieTarget,
        MacroTargets macroTargets)
    {
        Name = name;
        Description = description;
        DailyCalorieTarget = dailyCalorieTarget;
        MacroTargets = macroTargets;
        UpdatedAt = DateTime.UtcNow;
    }
}
