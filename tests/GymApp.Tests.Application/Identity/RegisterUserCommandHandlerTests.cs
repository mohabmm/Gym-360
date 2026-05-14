using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Commands.RegisterUser;
using GymApp.Modules.Identity.Application.Contracts;
using GymApp.Modules.Identity.Application.DTOs;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Errors;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Domain.ValueObjects;
using GymApp.Modules.Identity.Application.Abstractions;

namespace GymApp.Tests.Application.Identity;

public class RegisterUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserConsentRepository _userConsentRepository = Substitute.For<IUserConsentRepository>();
    private readonly IIdentityUnitOfWork _unitOfWork = Substitute.For<IIdentityUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly RegisterUserCommandHandler _sut;

    public RegisterUserCommandHandlerTests()
    {
        _sut = new RegisterUserCommandHandler(
            _userRepository, _userConsentRepository, _unitOfWork, _passwordHasher, _jwtTokenService);
    }

    private static RegisterUserCommand CreateValidCommand() =>
        new("test@example.com", "Password123!", "John", "Doe", "+201234567890", "Trainee");

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithTokens()
    {
        // Arrange
        var command = CreateValidCommand();
        _userRepository.ExistsAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _passwordHasher.Hash(command.Password).Returns("hashed_password");
        _jwtTokenService.GenerateAccessTokenAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns("access_token");
        _jwtTokenService.GenerateRefreshTokenAsync(Arg.Any<CancellationToken>())
            .Returns("refresh_token");
        _jwtTokenService.GetRefreshTokenExpiryTime().Returns(DateTime.UtcNow.AddDays(7));
        _jwtTokenService.GetAccessTokenExpiryTime().Returns(DateTime.UtcNow.AddHours(1));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access_token");
        result.Value.RefreshToken.Should().Be("refresh_token");
        await _userRepository.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        _userRepository.ExistsAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.EmailAlreadyExists.Code);
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidEmail_ReturnsFailure()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "invalid-email", "Password123!", "John", "Doe", "+201234567890", "Trainee");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserErrors.InvalidEmail.Code);
    }

    [Fact]
    public async Task Handle_InvalidRole_ReturnsFailure()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "test@example.com", "Password123!", "John", "Doe", "+201234567890", "InvalidRole");
        _userRepository.ExistsAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.InvalidRole");
    }
}
