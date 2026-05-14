using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Commands.SuspendGym;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using GymApp.Modules.Gyms.Application.Abstractions;

namespace GymApp.Tests.Application.Gyms;

public class SuspendGymCommandHandlerTests
{
    private readonly IGymRepository _gymRepository = Substitute.For<IGymRepository>();
    private readonly IGymsUnitOfWork _unitOfWork = Substitute.For<IGymsUnitOfWork>();
    private readonly SuspendGymCommandHandler _sut;

    public SuspendGymCommandHandlerTests()
    {
        _sut = new SuspendGymCommandHandler(_gymRepository, _unitOfWork);
    }

    private static Gym CreateActiveGym()
    {
        var address = new Address("123 Main St", "Cairo", "Cairo", "Egypt", "12345", null, null);
        var contact = new ContactInfo("gym@example.com", "+201234567890", null);
        var gymCode = GymCode.Generate();
        var gym = Gym.Create(Guid.NewGuid(), "Test Gym", "Description", address, contact, gymCode);
        gym.Approve(); // Move to Active state
        return gym;
    }

    [Fact]
    public async Task Handle_ActiveGym_SuspendsSuccessfully()
    {
        // Arrange
        var gymId = Guid.NewGuid();
        var gym = CreateActiveGym();
        _gymRepository.GetByIdAsync(gymId, Arg.Any<CancellationToken>()).Returns(gym);

        var command = new SuspendGymCommand(gymId, "Non-payment");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _gymRepository.Received(1).Update(gym);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GymNotFound_ReturnsFailure()
    {
        // Arrange
        var gymId = Guid.NewGuid();
        _gymRepository.GetByIdAsync(gymId, Arg.Any<CancellationToken>())
            .Returns((Gym?)null);

        var command = new SuspendGymCommand(gymId, "Non-payment");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(GymErrors.NotFound.Code);
    }
}
