using GymApp.Common.Domain;
using GymApp.Modules.Nutrition.Domain.Errors;
using GymApp.Modules.Nutrition.Domain.Events;

namespace GymApp.Modules.Nutrition.Domain.Entities;

public sealed class WaterLog : Entity
{
    private readonly List<WaterEntry> _entries = new();

    private WaterLog() { }

    public Guid UserId { get; private set; }
    public Guid GymId { get; private set; }
    public DateOnly Date { get; private set; }
    public int TotalMl { get; private set; }
    public int TargetMl { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<WaterEntry> Entries => _entries.AsReadOnly();

    public static WaterLog Create(Guid userId, Guid gymId, DateOnly date, int targetMl = 2000)
    {
        return new WaterLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GymId = gymId,
            Date = date,
            TotalMl = 0,
            TargetMl = targetMl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public Result AddEntry(int amountMl)
    {
        if (amountMl <= 0)
            return Result.Failure(NutritionErrors.InvalidWaterAmount);

        var entry = WaterEntry.Create(Id, amountMl);
        _entries.Add(entry);
        TotalMl += amountMl;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WaterLoggedDomainEvent(Id, UserId));

        return Result.Success();
    }

    public Result SetTarget(int ml)
    {
        if (ml <= 0)
            return Result.Failure(NutritionErrors.InvalidWaterAmount);

        TargetMl = ml;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }
}
