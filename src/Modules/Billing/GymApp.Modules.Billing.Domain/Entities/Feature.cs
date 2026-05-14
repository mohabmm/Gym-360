using GymApp.Common.Domain;

namespace GymApp.Modules.Billing.Domain.Entities;

public sealed class Feature : Entity
{
    private Feature() { }

    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? Category { get; private set; }
    public bool IsActive { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Feature Create(
        string code,
        string name,
        string? description = null,
        string? category = null,
        int sortOrder = 0)
    {
        return new Feature
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            Category = category,
            IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string? description, string? category, int sortOrder)
    {
        Name = name;
        Description = description;
        Category = category;
        SortOrder = sortOrder;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
