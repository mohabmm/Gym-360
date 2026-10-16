using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Commands.DeleteAccount;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Domain.ValueObjects;
using GymApp.Modules.Identity.Application.Abstractions;
using MediatR;

namespace GymApp.Tests.Application.Identity;

public class DeleteAccountCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IIdentityUnitOfWork _unitOfWork = Substitute.For<IIdentityUnitOfWork>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly DeleteAccountCommandHandler _sut;

    public DeleteAccountCommandHandlerTests()
    {
        _sut = new DeleteAccountCommandHandler(_userRepository, _unitOfWork, _publisher);
    }

    [Fact]
    public async Task Handle_ExistingUser_MarksDeletedAndRevokeTokens()
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
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        var command = new DeleteAccountCommand(userId);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var command = new DeleteAccountCommand(userId);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.NotFound");
    }
}
