using GymApp.Common.Domain;
using GymApp.Modules.Billing.Domain.ValueObjects;

namespace GymApp.Modules.Billing.Domain.Entities;

public sealed class SessionPackageTemplate : Entity
{
    private SessionPackageTemplate() { }

    public Guid GymId { get; private set; }
    public string Name { get; private set; } = default!;
    public int SessionCount { get; private set; }
    public Money PricePerSession { get; private set; } = default!;
    public Money TotalPrice { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static SessionPackageTemplate Create(
        Guid gymId,
        string name,
        int sessionCount,
        Money pricePerSession,
        Money totalPrice)
    {
        return new SessionPackageTemplate
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            Name = name,
            SessionCount = sessionCount,
            PricePerSession = pricePerSession,
            TotalPrice = totalPrice,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, int sessionCount, Money pricePerSession, Money totalPrice)
    {
        Name = name;
        SessionCount = sessionCount;
        PricePerSession = pricePerSession;
        TotalPrice = totalPrice;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
