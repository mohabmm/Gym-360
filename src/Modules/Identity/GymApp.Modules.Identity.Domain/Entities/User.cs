using GymApp.Common.Domain;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Events;
using GymApp.Modules.Identity.Domain.ValueObjects;

namespace GymApp.Modules.Identity.Domain.Entities;

public sealed class User : Entity
{
    private readonly List<GymMembership> _gymMemberships = new();
    private readonly List<RefreshToken> _refreshTokens = new();

    private User() { }

    public Email Email { get; private set; } = default!;
    public PhoneNumber PhoneNumber { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string? ProfilePhotoUrl { get; private set; }
    public DateTime? DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }
    public Language Language { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Set when this User is the principal of an Owner aggregate (multi-gym tenant root).
    /// Trainees/trainers/staff bind to a single gym via <see cref="GymMemberships"/> instead.
    /// </summary>
    public Guid? OwnerId { get; private set; }

    public IReadOnlyList<GymMembership> GymMemberships => _gymMemberships.AsReadOnly();
    public IReadOnlyList<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public static User Create(
        Email email,
        PhoneNumber phoneNumber,
        string passwordHash,
        string firstName,
        string lastName,
        Gender gender,
        UserRole role,
        Language language = Language.Arabic)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PhoneNumber = phoneNumber,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Gender = gender,
            Role = role,
            Status = UserStatus.PendingVerification,
            Language = language,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.RaiseDomainEvent(new UserRegisteredDomainEvent(user.Id));

        return user;
    }

    public void UpdateProfile(string firstName, string lastName, string? profilePhotoUrl, DateTime? dateOfBirth, Language language)
    {
        FirstName = firstName;
        LastName = lastName;
        ProfilePhotoUrl = profilePhotoUrl;
        DateOfBirth = dateOfBirth;
        Language = language;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserProfileUpdatedDomainEvent(Id));
    }

    public void ChangePhoneNumber(PhoneNumber phoneNumber)
    {
        PhoneNumber = phoneNumber;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new UserProfileUpdatedDomainEvent(Id));
    }

    public void Activate()
    {
        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Suspend()
    {
        Status = UserStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserSuspendedDomainEvent(Id));
    }

    public void MarkDeleted()
    {
        Status = UserStatus.Deleted;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public Result LinkToGym(Guid gymId, UserRole role, string? section, string? memberNumber)
    {
        if (_gymMemberships.Any(m => m.GymId == gymId && m.Role == role && m.IsActive))
            return Result.Failure(Errors.UserErrors.AlreadyLinkedToGym);

        // One-Gym principal rule (CLAUDE.md Rule 11): Trainer/Trainee/GymStaff hold at
        // most one Active membership across ALL gyms — enforced in the DB by the filtered
        // unique index UX_user_gym_memberships_active_per_user. Mirror it here so callers
        // get a clean 409 instead of an unhandled 500 (DbUpdateException) at SaveChanges.
        // Owners scope across gyms via OwnerId, not memberships, so they are exempt.
        if (role is UserRole.Trainer or UserRole.Trainee or UserRole.GymStaff
            && _gymMemberships.Any(m => m.IsActive && m.GymId != gymId))
            return Result.Failure(Errors.UserErrors.AlreadyActiveInAnotherGym);

        // Trainees must always have a MemberNumber so the trainee app's QR check-in
        // works. Non-trainee roles (Owner/Staff/Trainer) don't need one.
        if (role == UserRole.Trainee && string.IsNullOrWhiteSpace(memberNumber))
            return Result.Failure(Errors.UserErrors.TraineeMembershipMissingMemberNumber);

        var membership = GymMembership.Create(gymId, role, DateTime.UtcNow, section, memberNumber);
        _gymMemberships.Add(membership);
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UserLinkedToGymDomainEvent(Id, gymId, role.ToString()));

        return Result.Success();
    }

    public void AddRefreshToken(RefreshToken token)
    {
        _refreshTokens.Add(token);
    }

    public void RevokeAllRefreshTokens()
    {
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
        {
            token.Revoke();
        }
    }

    public void RecordLogin()
    {
        RaiseDomainEvent(new UserLoggedInDomainEvent(Id));
    }

    public void LinkToOwner(Guid ownerId)
    {
        OwnerId = ownerId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UnlinkOwner()
    {
        OwnerId = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
