using GymApp.Common.Application.Messaging;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Application.Commands.CreateTrainerByStaff;
using GymApp.Modules.Identity.Application.Contracts;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Domain.ValueObjects;

namespace GymApp.Tests.Application.Identity;

public class CreateTrainerByStaffCommandHandlerTests
{
    private static readonly Guid GymId = Guid.NewGuid();

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IIdentityUnitOfWork _unitOfWork = Substitute.For<IIdentityUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IIntegrationEventPublisher _events = Substitute.For<IIntegrationEventPublisher>();
    private readonly CreateTrainerByStaffCommandHandler _sut;

    public CreateTrainerByStaffCommandHandlerTests()
    {
        _passwordHasher.Hash(Arg.Any<string>()).Returns(ci => $"hashed:{ci.Arg<string>()}");
        _sut = new CreateTrainerByStaffCommandHandler(_userRepository, _unitOfWork, _passwordHasher, _events);
    }

    private static CreateTrainerByStaffCommand Command() =>
        new(GymId, "Omar", "Coach", "01001596189", "omar@example.com");

    private static User ExistingUser(UserRole role, string passwordHash = "own-hash")
    {
        var user = User.Create(
            new NewUserDetails
            {
                Email = Email.Create("omar@example.com").Value,
                PhoneNumber = PhoneNumber.Create("+201001596189").Value,
                PasswordHash = passwordHash,
                FirstName = "Omar",
                LastName = "Coach",
                Gender = Gender.Male
            },
            role);
        user.Activate();
        return user;
    }

    private void PhoneBelongsTo(User? user) =>
        _userRepository.GetByPhoneAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>()).Returns(user);

    [Fact]
    public async Task Handle_NewTrainer_CreatesWithDefaultPasswordAndReturnsIt()
    {
        PhoneBelongsTo(null);
        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        User? added = null;
        await _userRepository.AddAsync(Arg.Do<User>(u => added = u), Arg.Any<CancellationToken>());

        var result = await _sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.InitialPassword.Should().Be("12345678");
        added.Should().NotBeNull();
        added!.PasswordHash.Should().Be("hashed:12345678");
        result.Value.UserId.Should().Be(added.Id);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingTrainerFromAnotherGym_LinksWithoutChangingPassword()
    {
        // Security: re-adding a coach by phone must never reset their password to the
        // well-known default, or any gym could log in as another gym's coach.
        var trainer = ExistingUser(UserRole.Trainer);
        PhoneBelongsTo(trainer);

        var result = await _sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(trainer.Id);
        result.Value.InitialPassword.Should().BeNull();
        trainer.PasswordHash.Should().Be("own-hash");
        _passwordHasher.DidNotReceive().Hash(Arg.Any<string>());
        trainer.GymMemberships.Should().Contain(m => m.GymId == GymId && m.IsActive && m.Role == UserRole.Trainer);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TrainerAlreadyAtThisGym_ReturnsThemWithoutSaving()
    {
        var trainer = ExistingUser(UserRole.Trainer);
        trainer.LinkToGym(GymId, UserRole.Trainer, section: null, memberNumber: null);
        PhoneBelongsTo(trainer);

        var result = await _sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.InitialPassword.Should().BeNull();
        trainer.PasswordHash.Should().Be("own-hash");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PhoneBelongsToTrainee_RejectsAsReserved()
    {
        PhoneBelongsTo(ExistingUser(UserRole.Trainee));

        var result = await _sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.PhoneReservedForOtherRole.Code);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
