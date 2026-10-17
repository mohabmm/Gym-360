using GymApp.Modules.Identity.Application.Queries.LookupTraineeByPhone;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Domain.ValueObjects;

namespace GymApp.Tests.Application.Identity;

public class LookupTraineeByPhoneQueryHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly LookupTraineeByPhoneQueryHandler _sut;

    public LookupTraineeByPhoneQueryHandlerTests()
    {
        _sut = new LookupTraineeByPhoneQueryHandler(_userRepository);
    }

    private static User TraineeUser()
    {
        var email = Email.Create("mohab@example.com").Value;
        var phone = PhoneNumber.Create("+201001596189").Value;
        return User.Create(
            new NewUserDetails
            {
                Email = email,
                PhoneNumber = phone,
                PasswordHash = "hash",
                FirstName = "Mohab",
                LastName = "Test",
                Gender = Gender.Male
            },
            UserRole.Trainee);
    }

    [Fact]
    public async Task Handle_TraineeFound_ReturnsIdentity()
    {
        _userRepository.GetByPhoneAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(TraineeUser());

        // Unnormalized Egyptian local format must still resolve (normalizer → E.164).
        var result = await _sut.Handle(new LookupTraineeByPhoneQuery("01001596189"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("Mohab");
        result.Value.Phone.Should().Be("+201001596189");
    }

    [Fact]
    public async Task Handle_NoMatch_ReturnsNotFound()
    {
        _userRepository.GetByPhoneAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await _sut.Handle(new LookupTraineeByPhoneQuery("01001596189"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.NotFound");
    }

    [Fact]
    public async Task Handle_MatchIsNotTrainee_ReturnsNotFound()
    {
        var email = Email.Create("trainer@example.com").Value;
        var phone = PhoneNumber.Create("+201001596189").Value;
        var trainer = User.Create(
            new NewUserDetails
            {
                Email = email,
                PhoneNumber = phone,
                PasswordHash = "hash",
                FirstName = "T",
                LastName = "R",
                Gender = Gender.Male
            },
            UserRole.Trainer);
        _userRepository.GetByPhoneAsync(Arg.Any<PhoneNumber>(), Arg.Any<CancellationToken>())
            .Returns(trainer);

        var result = await _sut.Handle(new LookupTraineeByPhoneQuery("01001596189"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.NotFound");
    }
}
