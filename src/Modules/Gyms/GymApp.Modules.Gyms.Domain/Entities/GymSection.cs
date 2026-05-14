using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Enums;

namespace GymApp.Modules.Gyms.Domain.Entities;

public sealed class GymSection : Entity
{
    private GymSection() : base() { }

    private GymSection(
        Guid id,
        Guid gymId,
        SectionType sectionType,
        string? name,
        bool isActive) : base(id)
    {
        GymId = gymId;
        SectionType = sectionType;
        Name = name;
        IsActive = isActive;
    }

    public Guid GymId { get; private set; }
    public SectionType SectionType { get; private set; }
    public string? Name { get; private set; }
    public bool IsActive { get; private set; }

    public static GymSection Create(Guid gymId, SectionType sectionType, string? name)
    {
        return new GymSection(Guid.NewGuid(), gymId, sectionType, name, isActive: true);
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public void Update(string? name, SectionType sectionType)
    {
        Name = name;
        SectionType = sectionType;
    }
}
