using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.Events;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Domain.Entities;

public sealed class Gym : Entity
{
    private readonly List<DaySchedule> _operatingHours = new();
    private readonly List<string> _amenities = new();
    private readonly List<MemberId> _memberIds = new();
    private readonly List<TrainerId> _trainerIds = new();

    private Gym() : base() { }

    private Gym(Guid id) : base(id) { }

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
    public IReadOnlyList<MemberId> MemberIds => _memberIds.AsReadOnly();
    public IReadOnlyList<TrainerId> TrainerIds => _trainerIds.AsReadOnly();

    public static Gym Create(Guid ownerId, GymDetails details)
        => Create(Guid.NewGuid(), ownerId, details);

    public static Gym Create(Guid id, Guid ownerId, GymDetails details)
    {
        var gym = new Gym(id)
        {
            OwnerId = ownerId,
            Name = details.Name,
            Description = details.Description,
            Address = details.Address,
            ContactInfo = details.ContactInfo,
            GymCode = details.GymCode,
            Status = GymStatus.PendingApproval,
            PaymentStatus = PaymentStatus.Paid,
            Branding = new Branding(),
            SocialLinks = new SocialLinks(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        gym.RaiseDomainEvent(new GymRegisteredDomainEvent(gym.Id));
        return gym;
    }

    public Result Approve() =>
        ResolvePendingApproval(GymStatus.Active, new GymApprovedDomainEvent(Id));

    public Result Reject(string reason)
    {
        var result = ResolvePendingApproval(GymStatus.Rejected, new GymRejectedDomainEvent(Id, reason));
        if (result.IsSuccess)
            SuspensionReason = reason;
        return result;
    }

    private Result ResolvePendingApproval(GymStatus outcome, IDomainEvent domainEvent)
    {
        if (Status != GymStatus.PendingApproval)
            return Result.Failure(Errors.GymErrors.NotPendingApproval);

        Status = outcome;
        Touch();
        RaiseDomainEvent(domainEvent);
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
        Touch();
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
        Touch();
        RaiseDomainEvent(new GymReactivatedDomainEvent(Id));
        return Result.Success();
    }

    public Result Terminate()
    {
        if (Status == GymStatus.Terminated)
            return Result.Failure(Errors.GymErrors.GymTerminated);

        Status = GymStatus.Terminated;
        Touch();
        return Result.Success();
    }

    public Result MarkOverdue()
    {
        if (Status != GymStatus.Active)
            return Result.Failure(Errors.GymErrors.NotApproved);

        PaymentStatus = PaymentStatus.Overdue;
        Touch();
        return Result.Success();
    }

    public Result EnterGracePeriod(DateTime endsAt)
    {
        if (PaymentStatus != PaymentStatus.Overdue)
            return Result.Failure(Errors.GymErrors.AlreadyActive);

        Status = GymStatus.GracePeriod;
        PaymentStatus = PaymentStatus.GracePeriod;
        GracePeriodEndsAt = endsAt;
        Touch();
        return Result.Success();
    }

    public void UpdateBranding(Branding branding)
    {
        Branding = branding;
        Touch();
    }

    public void UpdateSocialLinks(SocialLinks socialLinks)
    {
        SocialLinks = socialLinks;
        Touch();
    }

    public void SetOperatingHours(List<DaySchedule> hours)
    {
        _operatingHours.Clear();
        _operatingHours.AddRange(hours);
        Touch();
    }

    public void SetAmenities(List<string> amenities)
    {
        _amenities.Clear();
        _amenities.AddRange(amenities);
        Touch();
    }

    public Result AddMember(MemberId memberId) =>
        AddToRoster(_memberIds, memberId, Errors.GymErrors.MemberAlreadyAdded, new MemberAddedToGymDomainEvent(Id, memberId.Value));

    public Result RemoveMember(MemberId memberId)
    {
        if (!_memberIds.Contains(memberId))
            return Result.Failure(Errors.GymErrors.MemberNotFound);

        _memberIds.Remove(memberId);
        Touch();
        RaiseDomainEvent(new MemberRemovedFromGymDomainEvent(Id, memberId.Value));
        return Result.Success();
    }

    public Result AddTrainer(TrainerId trainerId) =>
        AddToRoster(_trainerIds, trainerId, Errors.GymErrors.TrainerAlreadyAdded, new TrainerAddedToGymDomainEvent(Id, trainerId.Value));

    private Result AddToRoster<TId>(List<TId> roster, TId personId, Error alreadyAdded, IDomainEvent domainEvent)
        where TId : struct
    {
        if (Status != GymStatus.Active)
            return Result.Failure(Errors.GymErrors.NotApproved);

        if (roster.Contains(personId))
            return Result.Failure(alreadyAdded);

        roster.Add(personId);
        Touch();
        RaiseDomainEvent(domainEvent);
        return Result.Success();
    }

    public Result RemoveTrainer(TrainerId trainerId)
    {
        if (!_trainerIds.Contains(trainerId))
            return Result.Success();

        _trainerIds.Remove(trainerId);
        Touch();
        return Result.Success();
    }

    public void UpdateDetails(string name, string? description, Address address, ContactInfo contactInfo)
    {
        Name = name;
        Description = description;
        Address = address;
        ContactInfo = contactInfo;
        Touch();
    }

    public void SetKioskPin(string pinHash)
    {
        KioskPinHash = pinHash;
        Touch();
        RaiseDomainEvent(new GymKioskPinSetDomainEvent(Id));
    }

    public void ClearKioskPin()
    {
        KioskPinHash = null;
        Touch();
        RaiseDomainEvent(new GymKioskPinClearedDomainEvent(Id));
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
