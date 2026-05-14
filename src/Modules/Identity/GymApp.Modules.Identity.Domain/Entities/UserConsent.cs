using GymApp.Common.Domain;

namespace GymApp.Modules.Identity.Domain.Entities;

public sealed class UserConsent : Entity
{
    private UserConsent() { }

    public Guid UserId { get; private set; }
    public string ConsentType { get; private set; } = default!;
    public bool IsGranted { get; private set; }
    public DateTime GrantedAt { get; private set; }
    public DateTime? WithdrawnAt { get; private set; }
    public string ConsentText { get; private set; } = default!;
    public string IpAddress { get; private set; } = default!;

    public static UserConsent Create(Guid userId, string consentType, string consentText, string ipAddress)
    {
        return new UserConsent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ConsentType = consentType,
            IsGranted = true,
            GrantedAt = DateTime.UtcNow,
            ConsentText = consentText,
            IpAddress = ipAddress
        };
    }

    public void Withdraw()
    {
        IsGranted = false;
        WithdrawnAt = DateTime.UtcNow;
    }

    public void Grant(string consentText, string ipAddress)
    {
        IsGranted = true;
        GrantedAt = DateTime.UtcNow;
        WithdrawnAt = null;
        ConsentText = consentText;
        IpAddress = ipAddress;
    }
}
