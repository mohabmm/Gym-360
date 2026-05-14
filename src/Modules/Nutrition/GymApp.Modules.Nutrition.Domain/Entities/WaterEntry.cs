using GymApp.Common.Domain;

namespace GymApp.Modules.Nutrition.Domain.Entities;

public sealed class WaterEntry : Entity
{
    private WaterEntry() { }

    public Guid WaterLogId { get; private set; }
    public int AmountMl { get; private set; }
    public DateTime LoggedAt { get; private set; }

    public static WaterEntry Create(Guid waterLogId, int amountMl)
    {
        return new WaterEntry
        {
            Id = Guid.NewGuid(),
            WaterLogId = waterLogId,
            AmountMl = amountMl,
            LoggedAt = DateTime.UtcNow
        };
    }
}
