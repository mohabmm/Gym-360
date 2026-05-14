using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.Events;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Domain.Entities;

public sealed class Gym : Entity
{
    private readonly List<DaySchedule> _operatingHours = new();
    private readonly List<string> _amenities = new();
    private readonly List<Guid> _memberIds = new();
    private readonly List<Guid> _trainerIds = new();

    private Gym() : base() { }

    private Gym(
        Guid id,
        Guid ownerId,
        string name,
        string? description,
        Address address,
        ContactInfo contactInfo,
        GymCode gymCode) : base(id)
    {
        OwnerId = ownerId;
        Name = name;
        Description = description;
        Address = address;
        ContactInfo = contactInfo;
        GymCode = gymCode;
        Status = GymStatus.PendingApproval;
        PaymentStatus = PaymentStatus.Paid;
        Branding = new Branding();
        SocialLinks = new SocialLinks();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public Address Address { get; private set; } = default!;
    public ContactInfo ContactInfo { get; private set; } = default!;
    public IReadOnlyList<DaySchedule> OperatingHours => _operatingHours.AsReadOnly();
    public Branding Branding { get; private set; } = default!;
    public GymCode GymCode { get; private set; } = default!;
    public GymStatus Status { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public IReadOnlyList<string> Amenities => _amenities.AsReadOnly();
    public SocialLinks SocialLinks { get; private set; } = default!;
    public DateTime? SuspendedAt { get; private set; }
    public string? SuspensionReason { get; private set; }
    public DateTime? GracePeriodEndsAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string? KioskPinHash { get; private set; }
    public IReadOnlyList<Guid> MemberIds => _memberIds.AsReadOnly();
    public IReadOnlyList<Guid> TrainerIds => _trainerIds.AsReadOnly();

    public static Gym Create(
        Guid ownerId,
        string name,
        string? description,
        Address address,
        ContactInfo contactInfo,
        GymCode gymCode)
        => Create(Guid.NewGuid(), ownerId, name, description, address, contactInfo, gymCode);

    public static Gym Create(
        Guid id,
        Guid ownerId,
        string name,
        string? description,
        Address address,
        ContactInfo contactInfo,
        GymCode gymCode)
    {
        var gym = new Gym(
            id,
            ownerId,
            name,
            description,
            address,
            contactInfo,
            gymCode);

        gym.RaiseDomainEvent(new GymRegisteredDomainEvent(gym.Id));
        return gym;
    }

    public Result Approve()
    {
        if (Status != GymStatus.PendingApproval)
            return Result.Failure(Errors.GymErrors.NotPendingApproval);

        Status = GymStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new GymApprovedDomainEvent(Id));
        return Result.Success();
    }

    public Result Reject(string reason)
    {
        if (Status != GymStatus.PendingApproval)
            return Result.Failure(Errors.GymErrors.NotPendingApproval);

        Status = GymStatus.Rejected;
        SuspensionReason = reason;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new GymRejectedDomainEvent(Id, reason));
        return Result.Success();
    }

    public Result Suspend(string reason)
    {
        if (Status == GymStatus.Suspended)
            return Result.Failure(Errors.GymErrors.AlreadySuspended);

        if (Status == GymStatus.Terminated)
            return Result.Failure(Errors.GymErrors.GymTerminated);

        Status = GymStatus.Suspended;
        PaymentStatus = PaymentStatus.Suspended;
        SuspendedAt = DateTime.UtcNow;
        SuspensionReason = reason;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new GymSuspendedDomainEvent(Id, reason));
        return Result.Success();
    }

    public Result Reactivate()
    {
        if (Status != GymStatus.Suspended && Status != GymStatus.GracePeriod)
            return Result.Failure(Errors.GymErrors.AlreadyActive);

        Status = GymStatus.Active;
        PaymentStatus = PaymentStatus.Paid;
        SuspendedAt = null;
        SuspensionReason = null;
        GracePeriodEndsAt = null;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new GymReactivatedDomainEvent(Id));
        return Result.Success();
    }

    public Result Terminate()
    {
        if (Status == GymStatus.Terminated)
            return Result.Failure(Errors.GymErrors.GymTerminated);

        Status = GymStatus.Terminated;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result MarkOverdue()
    {
        if (Status != GymStatus.Active)
            return Result.Failure(Errors.GymErrors.NotApproved);

        PaymentStatus = PaymentStatus.Overdue;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result EnterGracePeriod(DateTime endsAt)
    {
        if (PaymentStatus != PaymentStatus.Overdue)
            return Result.Failure(Errors.GymErrors.AlreadyActive);

        Status = GymStatus.GracePeriod;
        PaymentStatus = PaymentStatus.GracePeriod;
        GracePeriodEndsAt = endsAt;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public void UpdateBranding(Branding branding)
    {
        Branding = branding;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSocialLinks(SocialLinks socialLinks)
    {
        SocialLinks = socialLinks;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetOperatingHours(List<DaySchedule> hours)
    {
        _operatingHours.Clear();
        _operatingHours.AddRange(hours);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAmenities(List<string> amenities)
    {
        _amenities.Clear();
        _amenities.AddRange(amenities);
        UpdatedAt = DateTime.UtcNow;
    }

    public Result AddMember(Guid memberId)
    {
        if (Status != GymStatus.Active)
            return Result.Failure(Errors.GymErrors.NotApproved);

        if (_memberIds.Contains(memberId))
            return Result.Failure(Errors.GymErrors.MemberAlreadyAdded);

        _memberIds.Add(memberId);
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new MemberAddedToGymDomainEvent(Id, memberId));
        return Result.Success();
    }

    public Result RemoveMember(Guid memberId)
    {
        if (!_memberIds.Contains(memberId))
            return Result.Failure(Errors.GymErrors.MemberNotFound);

        _memberIds.Remove(memberId);
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new MemberRemovedFromGymDomainEvent(Id, memberId));
        return Result.Success();
    }

    public Result AddTrainer(Guid trainerId)
    {
        if (Status != GymStatus.Active)
            return Result.Failure(Errors.GymErrors.NotApproved);

        if (_trainerIds.Contains(trainerId))
            return Result.Failure(Errors.GymErrors.TrainerAlreadyAdded);

        _trainerIds.Add(trainerId);
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new TrainerAddedToGymDomainEvent(Id, trainerId));
        return Result.Success();
    }

    public Result RemoveTrainer(Guid trainerId)
    {
        if (!_trainerIds.Contains(trainerId))
            return Result.Success();

        _trainerIds.Remove(trainerId);
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public void UpdateDetails(string name, string? description, Address address, ContactInfo contactInfo)
    {
        Name = name;
        Description = description;
        Address = address;
        ContactInfo = contactInfo;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetKioskPin(string pinHash)
    {
        KioskPinHash = pinHash;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new GymKioskPinSetDomainEvent(Id));
    }

    public void ClearKioskPin()
    {
        KioskPinHash = null;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new GymKioskPinClearedDomainEvent(Id));
    }
}
