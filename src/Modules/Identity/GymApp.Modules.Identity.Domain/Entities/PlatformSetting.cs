using GymApp.Common.Domain;

namespace GymApp.Modules.Identity.Domain.Entities;

public sealed class PlatformSetting : Entity
{
    private PlatformSetting() { }

    public string Key { get; private set; } = default!;
    public string? Value { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public static PlatformSetting Create(string key, string? value, Guid? updatedBy)
    {
        return new PlatformSetting
        {
            Id = Guid.NewGuid(),
            Key = key,
            Value = value,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = updatedBy
        };
    }

    public void SetValue(string? value, Guid? updatedBy)
    {
        Value = value;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}
