using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Commands.RecordConsent;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Domain.ValueObjects;
using GymApp.Modules.Identity.Application.Abstractions;

namespace GymApp.Tests.Application.Identity;

public class RecordConsentCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserConsentRepository _consentRepository = Substitute.For<IUserConsentRepository>();
    private readonly IIdentityUnitOfWork _unitOfWork = Substitute.For<IIdentityUnitOfWork>();
    private readonly RecordConsentCommandHandler _sut;

    public RecordConsentCommandHandlerTests()
    {
        _sut = new RecordConsentCommandHandler(_userRepository, _consentRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_NewConsent_CreatesConsentRecord()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = User.Create(
            new NewUserDetails
            {
                Email = Email.Create("test@example.com").Value,
                PhoneNumber = PhoneNumber.Create("+201234567890").Value,
                PasswordHash = "hashed",
                FirstName = "John",
                LastName = "Doe",
                Gender = Gender.Male
            },
            UserRole.Trainee);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _consentRepository.GetByUserIdAndTypeAsync(userId, "marketing", Arg.Any<CancellationToken>())
            .Returns((UserConsent?)null);

        var command = new RecordConsentCommand(userId, "marketing", true, "I agree", "192.168.1.1");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _consentRepository.Received(1).AddAsync(Arg.Any<UserConsent>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingConsentWithdraw_UpdatesConsent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = User.Create(
            new NewUserDetails
            {
                Email = Email.Create("test@example.com").Value,
                PhoneNumber = PhoneNumber.Create("+201234567890").Value,
                PasswordHash = "hashed",
                FirstName = "John",
                LastName = "Doe",
                Gender = Gender.Male
            },
            UserRole.Trainee);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var existingConsent = UserConsent.Create(userId, "marketing", "I agree", "192.168.1.1");
        _consentRepository.GetByUserIdAndTypeAsync(userId, "marketing", Arg.Any<CancellationToken>())
            .Returns(existingConsent);

        var command = new RecordConsentCommand(userId, "marketing", false, "Withdrawing", "192.168.1.1");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _consentRepository.Received(1).Update(existingConsent);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var command = new RecordConsentCommand(userId, "marketing", true, "I agree", "192.168.1.1");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.NotFound");
    }
}
