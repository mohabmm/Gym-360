using GymApp.Common.Domain;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Events;
using GymApp.Modules.Identity.Domain.ValueObjects;

namespace GymApp.Tests.Domain.Identity;

public sealed class UserTests
{
    private static Email CreateValidEmail() => Email.Create("test@example.com").Value;
    private static PhoneNumber CreateValidPhone() => PhoneNumber.Create("+201234567890").Value;

    private static User CreateDefaultUser(
        UserRole role = UserRole.Trainee,
        Language language = Language.Arabic)
    {
        return User.Create(
            new NewUserDetails
            {
                Email = CreateValidEmail(),
                PhoneNumber = CreateValidPhone(),
                PasswordHash = "hashed_password_123",
                FirstName = "Ahmed",
                LastName = "Mohamed",
                Gender = Gender.Male,
                Language = language
            },
            role);
    }

    [Fact]
    public void Create_ValidInput_ShouldCreateUserWithCorrectProperties()
    {
        var email = CreateValidEmail();
        var phone = CreateValidPhone();

        var user = User.Create(
            new NewUserDetails
            {
                Email = email,
                PhoneNumber = phone,
                PasswordHash = "hash",
                FirstName = "Ali",
                LastName = "Hassan",
                Gender = Gender.Male,
                Language = Language.Arabic
            },
            UserRole.Trainee);

        user.Id.Should().NotBeEmpty();
        user.Email.Should().Be(email);
        user.PhoneNumber.Should().Be(phone);
        user.FirstName.Should().Be("Ali");
        user.LastName.Should().Be("Hassan");
        user.Gender.Should().Be(Gender.Male);
        user.Role.Should().Be(UserRole.Trainee);
        user.Language.Should().Be(Language.Arabic);
        user.Status.Should().Be(UserStatus.PendingVerification);
    }

    [Fact]
    public void Create_DefaultLanguage_ShouldBeArabic()
    {
        var user = User.Create(
            new NewUserDetails
            {
                Email = CreateValidEmail(),
                PhoneNumber = CreateValidPhone(),
                PasswordHash = "hash",
                FirstName = "Ali",
                LastName = "Hassan",
                Gender = Gender.Male
            },
            UserRole.Trainee);

        user.Language.Should().Be(Language.Arabic);
    }

    [Fact]
    public void Create_ShouldRaiseUserRegisteredDomainEvent()
    {
        var user = CreateDefaultUser();

        var domainEvents = user.GetDomainEvents();
        domainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserRegisteredDomainEvent>()
            .Which.UserId.Should().Be(user.Id);
    }

    [Fact]
    public void UpdateProfile_ShouldUpdateFieldsAndRaiseEvent()
    {
        var user = CreateDefaultUser();
        user.ClearDomainEvents();

        user.UpdateProfile(new UserProfile("NewFirst", "NewLast", "http://photo.jpg", new DateTime(1990, 1, 1), Language.English));

        user.FirstName.Should().Be("NewFirst");
        user.LastName.Should().Be("NewLast");
        user.ProfilePhotoUrl.Should().Be("http://photo.jpg");
        user.DateOfBirth.Should().Be(new DateTime(1990, 1, 1));
        user.Language.Should().Be(Language.English);
        user.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<UserProfileUpdatedDomainEvent>();
    }

    [Fact]
    public void Activate_ShouldSetStatusToActive()
    {
        var user = CreateDefaultUser();

        user.Activate();

        user.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public void Suspend_ShouldSetStatusAndRaiseEvent()
    {
        var user = CreateDefaultUser();
        user.ClearDomainEvents();

        user.Suspend();

        user.Status.Should().Be(UserStatus.Suspended);
        user.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<UserSuspendedDomainEvent>();
    }

    [Fact]
    public void MarkDeleted_ShouldSetStatusToDeleted()
    {
        var user = CreateDefaultUser();

        user.MarkDeleted();

        user.Status.Should().Be(UserStatus.Deleted);
    }

    [Fact]
    public void UpdatePasswordHash_ShouldUpdatePassword()
    {
        var user = CreateDefaultUser();

        user.UpdatePasswordHash("new_hash");

        user.PasswordHash.Should().Be("new_hash");
    }

    private sealed record MemberNumbers(string? First, string? Second);

    private static void AssertFailedWith(Result result, string expectedCode)
    {
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(expectedCode);
    }

    // Links a default trainee to a gym, then links the same gym again with the given role.
    private static (User User, Result Second) LinkTraineeThenRelinkSameGym(UserRole role, string? memberNumber)
    {
        var user = CreateDefaultUser();
        var gymId = Guid.NewGuid();
        user.LinkToGym(gymId, UserRole.Trainee, null, "MEM001");

        return (user, user.LinkToGym(gymId, role, null, memberNumber));
    }

    // Creates a user with the role and links it, in that role, to two different gyms.
    private static (User User, Result Second) LinkToTwoGyms(UserRole role, MemberNumbers? memberNumbers = null)
    {
        var user = CreateDefaultUser(role);
        user.LinkToGym(Guid.NewGuid(), role, null, memberNumbers?.First);

        return (user, user.LinkToGym(Guid.NewGuid(), role, null, memberNumbers?.Second));
    }

    [Fact]
    public void LinkToGym_ValidGym_ShouldAddMembershipAndRaiseEvent()
    {
        var user = CreateDefaultUser();
        user.ClearDomainEvents();
        var gymId = Guid.NewGuid();

        var result = user.LinkToGym(gymId, UserRole.Trainee, "Mixed", "MEM001");

        result.IsSuccess.Should().BeTrue();
        user.GymMemberships.Should().ContainSingle();
        user.GymMemberships[0].GymId.Should().Be(gymId);
        user.GymMemberships[0].IsActive.Should().BeTrue();
        user.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<UserLinkedToGymDomainEvent>();
    }

    [Fact]
    public void LinkToGym_DuplicateActiveLink_ShouldReturnFailure()
    {
        var (_, result) = LinkTraineeThenRelinkSameGym(UserRole.Trainee, "MEM001");

        AssertFailedWith(result, "User.AlreadyLinkedToGym");
    }

    [Fact]
    public void LinkToGym_SameGymDifferentRole_ShouldSucceed()
    {
        var (user, result) = LinkTraineeThenRelinkSameGym(UserRole.Trainer, null);

        result.IsSuccess.Should().BeTrue();
        user.GymMemberships.Should().HaveCount(2);
    }

    [Fact]
    public void LinkToGym_TraineeWithoutMemberNumber_ShouldReturnFailure()
    {
        var user = CreateDefaultUser();
        var gymId = Guid.NewGuid();

        var result = user.LinkToGym(gymId, UserRole.Trainee, null, memberNumber: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.TraineeMembershipMissingMemberNumber");
        user.GymMemberships.Should().BeEmpty();
    }

    [Fact]
    public void LinkToGym_NonTraineeRoleWithoutMemberNumber_ShouldSucceed()
    {
        var user = CreateDefaultUser();
        var gymId = Guid.NewGuid();

        var result = user.LinkToGym(gymId, UserRole.Trainer, null, memberNumber: null);

        result.IsSuccess.Should().BeTrue();
        user.GymMemberships.Should().ContainSingle();
    }

    // Regression: SigNoz-confirmed 500 on staff-create-trainer. A trainer Active
    // at gym A, re-linked to gym B (reuse path), used to add a 2nd active membership
    // and trip the DB filtered unique index → unhandled DbUpdateException → 500.
    // One-Gym rule (Rule 11) must reject in-domain with a clean conflict instead.
    [Fact]
    public void LinkToGym_TrainerAlreadyActiveInAnotherGym_ShouldReturnConflict()
    {
        var (user, result) = LinkToTwoGyms(UserRole.Trainer);

        AssertFailedWith(result, "User.AlreadyActiveInAnotherGym");
        user.GymMemberships.Should().ContainSingle(); // second gym not linked
    }

    [Fact]
    public void LinkToGym_TraineeAlreadyActiveInAnotherGym_ShouldReturnConflict() =>
        AssertFailedWith(
            LinkToTwoGyms(UserRole.Trainee, new MemberNumbers("MEM001", "MEM002")).Second,
            "User.AlreadyActiveInAnotherGym");

    [Fact]
    public void LinkToGym_StaffAlreadyActiveInAnotherGym_ShouldReturnConflict() =>
        AssertFailedWith(LinkToTwoGyms(UserRole.GymStaff).Second, "User.AlreadyActiveInAnotherGym");

    [Fact]
    public void LinkToGym_OwnerActiveInAnotherGym_IsExemptFromOneGymRule()
    {
        // Owners scope across many gyms via OwnerId, so the One-Gym membership
        // guard must NOT apply to the GymOwner role.
        var (user, result) = LinkToTwoGyms(UserRole.GymOwner);

        result.IsSuccess.Should().BeTrue();
        user.GymMemberships.Should().HaveCount(2);
    }

    [Fact]
    public void AddRefreshToken_ShouldAddToCollection()
    {
        var user = CreateDefaultUser();
        var token = RefreshToken.Create("token_value", DateTime.UtcNow.AddDays(7));

        user.AddRefreshToken(token);

        user.RefreshTokens.Should().ContainSingle();
        user.RefreshTokens[0].Token.Should().Be("token_value");
    }

    [Fact]
    public void RevokeAllRefreshTokens_ShouldRevokeActiveTokens()
    {
        var user = CreateDefaultUser();
        var token1 = RefreshToken.Create("t1", DateTime.UtcNow.AddDays(7));
        var token2 = RefreshToken.Create("t2", DateTime.UtcNow.AddDays(7));
        user.AddRefreshToken(token1);
        user.AddRefreshToken(token2);

        user.RevokeAllRefreshTokens();

        user.RefreshTokens.Should().AllSatisfy(t => t.IsRevoked.Should().BeTrue());
    }

    [Fact]
    public void RecordLogin_ShouldRaiseUserLoggedInDomainEvent()
    {
        var user = CreateDefaultUser();
        user.ClearDomainEvents();

        user.RecordLogin();

        user.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<UserLoggedInDomainEvent>();
    }
}

public sealed class EmailValueObjectTests
{
    [Fact]
    public void Create_ValidEmail_ShouldSucceed()
    {
        var result = Email.Create("user@example.com");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("user@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    [InlineData("@missing.com")]
    public void Create_InvalidEmail_ShouldFail(string value)
    {
        var result = Email.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.InvalidEmail");
    }

    [Fact]
    public void Create_ShouldNormalizeToLowerCase()
    {
        var result = Email.Create("User@EXAMPLE.COM");

        result.Value.Value.Should().Be("user@example.com");
    }
}

public sealed class PhoneNumberValueObjectTests
{
    [Fact]
    public void Create_ValidPhone_ShouldSucceed()
    {
        var result = PhoneNumber.Create("+201234567890");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("+201234567890");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("abc")]
    [InlineData("123")]
    public void Create_InvalidPhone_ShouldFail(string value)
    {
        var result = PhoneNumber.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.InvalidPhoneNumber");
    }
}

public sealed class RefreshTokenTests
{
    [Fact]
    public void Create_ShouldSetPropertiesCorrectly()
    {
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var token = RefreshToken.Create("my_token", expiresAt);

        token.Token.Should().Be("my_token");
        token.ExpiresAt.Should().Be(expiresAt);
        token.IsRevoked.Should().BeFalse();
        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Revoke_ShouldMarkAsRevoked()
    {
        var token = RefreshToken.Create("my_token", DateTime.UtcNow.AddDays(7));

        token.Revoke();

        token.IsRevoked.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenPastExpiry_ShouldReturnTrue()
    {
        var token = RefreshToken.Create("my_token", DateTime.UtcNow.AddSeconds(-1));

        token.IsExpired.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }
}
