using GymApp.Common.Domain;

namespace GymApp.Modules.Identity.Domain.Entities;

public sealed class Owner : Entity
{
    private Owner() { }

    private Owner(Guid id, Guid userId, string companyName, string billingEmail) : base(id)
    {
        UserId = userId;
        CompanyName = companyName;
        BillingEmail = billingEmail;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public string CompanyName { get; private set; } = default!;
    public string BillingEmail { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    public Contract? Contract { get; private set; }

    public static Owner Create(Guid userId, string companyName, string billingEmail)
    {
        return new Owner(Guid.NewGuid(), userId, companyName, billingEmail);
    }

    public void AttachContract(Contract contract)
    {
        Contract = contract;
    }

    public void UpdateBillingDetails(string companyName, string billingEmail)
    {
        CompanyName = companyName;
        BillingEmail = billingEmail;
    }
}
