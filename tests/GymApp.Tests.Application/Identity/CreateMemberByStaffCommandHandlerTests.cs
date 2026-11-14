using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Application.Commands.CreateMemberByStaff;
using GymApp.Modules.Identity.Application.Contracts;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Domain.ValueObjects;

namespace GymApp.Tests.Application.Identity;

public class CreateMemberByStaffCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IIdentityUnitOfWork _unitOfWork = Substitute.For<IIdentityUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly CreateMemberByStaffCommandHandler _sut;

    public CreateMemberByStaffCommandHandlerTests()
    {
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed");
        _sut = new CreateMemberByStaffCommandHandler(_userRepository, _unitOfWork, _passwordHasher);
    }

    private static CreateMemberByStaffCommand Command(string? email = "new@example.com") =>
        new("Mohab", "Test", "01001596189", email);

    private static User ExistingUser(UserRole role)
    {
        var email = Email.Create("someone.else@example.com").Value;
        var phone = PhoneNumber.Create("+201001596189").Value;
        return User.Create(
            new NewUserDetails
            {
                Email = email,
                PhoneNumber = phone,
                PasswordHash = "hash",
                FirstName = "Someone",
                LastName = "Else",
                Gender = Gender.Male
            },
            role);
    }

    [Fact]
    public async Task Handle_NewPhoneAndEmail_CreatesMember()
    {
        _userRepository.GetByPhoneAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await _sut.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _userRepository.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewMember_GetsFixedDefaultPassword()
    {
        _userRepository.GetByPhoneAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        await _sut.Handle(Command(), CancellationToken.None);

        _passwordHasher.Received(1).Hash("12345678");
    }

    [Fact]
    public async Task Handle_PhoneBelongsToExistingTrainee_RejectsWithoutCreatingOrMerging()
    {
        // The reported bug: staff-create used to silently return the existing trainee,
        // surfacing a different person's identity. It must now reject with 409 instead.
        _userRepository.GetByPhoneAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(ExistingUser(UserRole.Trainee));

        var result = await _sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.MemberPhoneAlreadyExists.Code);
        // No silent reuse: the existing account is neither mutated nor re-saved.
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        _userRepository.DidNotReceive().Update(Arg.Any<User>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PhoneBelongsToNonTrainee_RejectsAsReserved()
    {
        _userRepository.GetByPhoneAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(ExistingUser(UserRole.Trainer));

        var result = await _sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.PhoneReservedForOtherRole.Code);
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmailBelongsToExistingTrainee_Rejects()
    {
        _userRepository.GetByPhoneAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _userRepository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(ExistingUser(UserRole.Trainee));

        var result = await _sut.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.MemberEmailAlreadyExists.Code);
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}

public class CreateMemberByStaffCommandValidatorTests
{
    private readonly CreateMemberByStaffCommandValidator _validator = new();

    private static CreateMemberByStaffCommand With(string? email) => new("Sara", "Member", "01001596189", email);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Email_is_required_because_members_sign_in_with_it(string? email) =>
        _validator.Validate(With(email)).IsValid.Should().BeFalse();

    [Fact]
    public void Valid_email_passes() =>
        _validator.Validate(With("sara@example.com")).IsValid.Should().BeTrue();
}
