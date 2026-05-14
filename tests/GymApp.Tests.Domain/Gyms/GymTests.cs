using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.Events;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Tests.Domain.Gyms;

public sealed class GymTests
{
    private static Gym CreateDefaultGym()
    {
        return Gym.Create(
            Guid.NewGuid(),
            "Test Gym",
            "A test gym",
            new Address("123 St", "Cairo", "Cairo", "Egypt", "12345"),
            new ContactInfo("gym@test.com", "+201234567890"),
            GymCode.Create("ABC123"));
    }

    private static Gym CreateActiveGym()
    {
        var gym = CreateDefaultGym();
        gym.Approve();
        gym.ClearDomainEvents();
        return gym;
    }

    // --- Creation ---

    [Fact]
    public void Create_ValidInput_ShouldSetCorrectProperties()
    {
        var ownerId = Guid.NewGuid();

        var gym = Gym.Create(
            ownerId,
            "My Gym",
            "Description",
            new Address("St", "City", "State", "Country", "00000"),
            new ContactInfo("e@e.com", "+201111111111"),
            GymCode.Create("XYZ789"));

        gym.Id.Should().NotBeEmpty();
        gym.OwnerId.Should().Be(ownerId);
        gym.Name.Should().Be("My Gym");
        gym.Status.Should().Be(GymStatus.PendingApproval);
        gym.PaymentStatus.Should().Be(PaymentStatus.Paid);
    }

    [Fact]
    public void Create_ShouldRaiseGymRegisteredDomainEvent()
    {
        var gym = CreateDefaultGym();

        gym.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<GymRegisteredDomainEvent>();
    }

    // --- Approve ---

    [Fact]
    public void Approve_WhenPendingApproval_ShouldSetActiveAndRaiseEvent()
    {
        var gym = CreateDefaultGym();
        gym.ClearDomainEvents();

        var result = gym.Approve();

        result.IsSuccess.Should().BeTrue();
        gym.Status.Should().Be(GymStatus.Active);
        gym.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<GymApprovedDomainEvent>();
    }

    [Fact]
    public void Approve_WhenAlreadyActive_ShouldFail()
    {
        var gym = CreateActiveGym();

        var result = gym.Approve();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Gyms.NotPendingApproval");
    }

    // --- Suspension state machine ---

    [Fact]
    public void Suspend_WhenActive_ShouldSetSuspendedAndRaiseEvent()
    {
        var gym = CreateActiveGym();

        var result = gym.Suspend("Non-payment");

        result.IsSuccess.Should().BeTrue();
        gym.Status.Should().Be(GymStatus.Suspended);
        gym.PaymentStatus.Should().Be(PaymentStatus.Suspended);
        gym.SuspensionReason.Should().Be("Non-payment");
        gym.SuspendedAt.Should().NotBeNull();
        gym.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<GymSuspendedDomainEvent>();
    }

    [Fact]
    public void Suspend_WhenAlreadySuspended_ShouldFail()
    {
        var gym = CreateActiveGym();
        gym.Suspend("reason");

        var result = gym.Suspend("again");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Gyms.AlreadySuspended");
    }

    [Fact]
    public void Suspend_WhenTerminated_ShouldFail()
    {
        var gym = CreateActiveGym();
        gym.Terminate();

        var result = gym.Suspend("reason");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Gyms.GymTerminated");
    }

    [Fact]
    public void Reactivate_WhenSuspended_ShouldSetActiveAndClearSuspension()
    {
        var gym = CreateActiveGym();
        gym.Suspend("reason");
        gym.ClearDomainEvents();

        var result = gym.Reactivate();

        result.IsSuccess.Should().BeTrue();
        gym.Status.Should().Be(GymStatus.Active);
        gym.PaymentStatus.Should().Be(PaymentStatus.Paid);
        gym.SuspendedAt.Should().BeNull();
        gym.SuspensionReason.Should().BeNull();
        gym.GracePeriodEndsAt.Should().BeNull();
        gym.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<GymReactivatedDomainEvent>();
    }

    [Fact]
    public void Reactivate_WhenActive_ShouldFail()
    {
        var gym = CreateActiveGym();

        var result = gym.Reactivate();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Gyms.AlreadyActive");
    }

    [Fact]
    public void Terminate_WhenActive_ShouldSetTerminated()
    {
        var gym = CreateActiveGym();

        var result = gym.Terminate();

        result.IsSuccess.Should().BeTrue();
        gym.Status.Should().Be(GymStatus.Terminated);
    }

    [Fact]
    public void Terminate_WhenAlreadyTerminated_ShouldFail()
    {
        var gym = CreateActiveGym();
        gym.Terminate();

        var result = gym.Terminate();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Gyms.GymTerminated");
    }

    // --- Payment status flow: Active -> Overdue -> GracePeriod -> Suspended ---

    [Fact]
    public void MarkOverdue_WhenActive_ShouldSetOverdue()
    {
        var gym = CreateActiveGym();

        var result = gym.MarkOverdue();

        result.IsSuccess.Should().BeTrue();
        gym.PaymentStatus.Should().Be(PaymentStatus.Overdue);
    }

    [Fact]
    public void MarkOverdue_WhenNotActive_ShouldFail()
    {
        var gym = CreateDefaultGym(); // PendingApproval

        var result = gym.MarkOverdue();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void EnterGracePeriod_WhenOverdue_ShouldSetGracePeriod()
    {
        var gym = CreateActiveGym();
        gym.MarkOverdue();
        var endsAt = DateTime.UtcNow.AddDays(14);

        var result = gym.EnterGracePeriod(endsAt);

        result.IsSuccess.Should().BeTrue();
        gym.Status.Should().Be(GymStatus.GracePeriod);
        gym.PaymentStatus.Should().Be(PaymentStatus.GracePeriod);
        gym.GracePeriodEndsAt.Should().Be(endsAt);
    }

    [Fact]
    public void EnterGracePeriod_WhenNotOverdue_ShouldFail()
    {
        var gym = CreateActiveGym();

        var result = gym.EnterGracePeriod(DateTime.UtcNow.AddDays(14));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Reactivate_WhenInGracePeriod_ShouldSucceed()
    {
        var gym = CreateActiveGym();
        gym.MarkOverdue();
        gym.EnterGracePeriod(DateTime.UtcNow.AddDays(14));

        var result = gym.Reactivate();

        result.IsSuccess.Should().BeTrue();
        gym.Status.Should().Be(GymStatus.Active);
    }

    [Fact]
    public void FullSuspensionLifecycle_Active_Overdue_Grace_Suspended_Reactivated()
    {
        var gym = CreateActiveGym();

        gym.MarkOverdue().IsSuccess.Should().BeTrue();
        gym.EnterGracePeriod(DateTime.UtcNow.AddDays(14)).IsSuccess.Should().BeTrue();
        gym.Suspend("Grace period expired").IsSuccess.Should().BeTrue();
        gym.Reactivate().IsSuccess.Should().BeTrue();

        gym.Status.Should().Be(GymStatus.Active);
        gym.PaymentStatus.Should().Be(PaymentStatus.Paid);
    }

    // --- Members and Trainers ---

    [Fact]
    public void AddMember_WhenActive_ShouldSucceed()
    {
        var gym = CreateActiveGym();
        var memberId = Guid.NewGuid();

        var result = gym.AddMember(memberId);

        result.IsSuccess.Should().BeTrue();
        gym.MemberIds.Should().Contain(memberId);
        gym.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<MemberAddedToGymDomainEvent>();
    }

    [Fact]
    public void AddMember_WhenNotActive_ShouldFail()
    {
        var gym = CreateDefaultGym(); // PendingApproval

        var result = gym.AddMember(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Gyms.NotApproved");
    }

    [Fact]
    public void AddMember_Duplicate_ShouldFail()
    {
        var gym = CreateActiveGym();
        var memberId = Guid.NewGuid();
        gym.AddMember(memberId);

        var result = gym.AddMember(memberId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Gyms.MemberAlreadyAdded");
    }

    [Fact]
    public void RemoveMember_WhenExists_ShouldSucceed()
    {
        var gym = CreateActiveGym();
        var memberId = Guid.NewGuid();
        gym.AddMember(memberId);
        gym.ClearDomainEvents();

        var result = gym.RemoveMember(memberId);

        result.IsSuccess.Should().BeTrue();
        gym.MemberIds.Should().NotContain(memberId);
        gym.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<MemberRemovedFromGymDomainEvent>();
    }

    [Fact]
    public void RemoveMember_WhenNotExists_ShouldFail()
    {
        var gym = CreateActiveGym();

        var result = gym.RemoveMember(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Gyms.MemberNotFound");
    }

    [Fact]
    public void AddTrainer_WhenActive_ShouldSucceed()
    {
        var gym = CreateActiveGym();
        var trainerId = Guid.NewGuid();

        var result = gym.AddTrainer(trainerId);

        result.IsSuccess.Should().BeTrue();
        gym.TrainerIds.Should().Contain(trainerId);
        gym.GetDomainEvents().Should().ContainSingle()
            .Which.Should().BeOfType<TrainerAddedToGymDomainEvent>();
    }

    [Fact]
    public void AddTrainer_Duplicate_ShouldFail()
    {
        var gym = CreateActiveGym();
        var trainerId = Guid.NewGuid();
        gym.AddTrainer(trainerId);

        var result = gym.AddTrainer(trainerId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Gyms.TrainerAlreadyAdded");
    }

    // --- Update methods ---

    [Fact]
    public void UpdateDetails_ShouldUpdateAllFields()
    {
        var gym = CreateActiveGym();
        var newAddress = new Address("New St", "Alex", "Alex", "Egypt", "99999");
        var newContact = new ContactInfo("new@gym.com", "+201999999999");

        gym.UpdateDetails("New Name", "New Desc", newAddress, newContact);

        gym.Name.Should().Be("New Name");
        gym.Description.Should().Be("New Desc");
        gym.Address.Should().Be(newAddress);
        gym.ContactInfo.Should().Be(newContact);
    }

    [Fact]
    public void SetOperatingHours_ShouldReplaceHours()
    {
        var gym = CreateDefaultGym();
        var hours = new List<DaySchedule>
        {
            new(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(22, 0), false)
        };

        gym.SetOperatingHours(hours);

        gym.OperatingHours.Should().HaveCount(1);
    }

    [Fact]
    public void SetAmenities_ShouldReplaceAmenities()
    {
        var gym = CreateDefaultGym();

        gym.SetAmenities(new List<string> { "Pool", "Sauna" });

        gym.Amenities.Should().BeEquivalentTo(new[] { "Pool", "Sauna" });
    }
}

public sealed class GymCodeTests
{
    [Fact]
    public void Create_ValidCode_ShouldUppercase()
    {
        var code = GymCode.Create("abc123");

        code.Value.Should().Be("ABC123");
    }

    [Theory]
    [InlineData("")]
    [InlineData("AB")]
    [InlineData("ABCDEFG")]
    public void Create_InvalidLength_ShouldThrow(string value)
    {
        var act = () => GymCode.Create(value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_ShouldReturn6CharCode()
    {
        var code = GymCode.Generate();

        code.Value.Should().HaveLength(6);
    }
}
