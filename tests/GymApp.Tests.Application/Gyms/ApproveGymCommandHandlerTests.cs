using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Commands.ApproveGym;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using GymApp.Modules.Gyms.Application.Abstractions;
using MediatR;

namespace GymApp.Tests.Application.Gyms;

public class ApproveGymCommandHandlerTests
{
    private readonly IGymRepository _gymRepository = Substitute.For<IGymRepository>();
    private readonly IGymsUnitOfWork _unitOfWork = Substitute.For<IGymsUnitOfWork>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ApproveGymCommandHandler _sut;

    public ApproveGymCommandHandlerTests()
    {
        _sut = new ApproveGymCommandHandler(_gymRepository, _unitOfWork, _publisher);
    }

    private static Gym CreatePendingGym()
    {
        var address = new Address("123 Main St", "Cairo", "Cairo", "Egypt", "12345", null, null);
        var contact = new ContactInfo("gym@example.com", "+201234567890", null);
        var gymCode = GymCode.Generate();
        return Gym.Create(Guid.NewGuid(), "Test Gym", "Description", address, contact, gymCode);
    }

    [Fact]
    public async Task Handle_PendingGym_ApprovesSuccessfully()
    {
        // Arrange
        var gymId = Guid.NewGuid();
        var gym = CreatePendingGym();
        _gymRepository.GetByIdAsync(gymId, Arg.Any<CancellationToken>()).Returns(gym);

        var command = new ApproveGymCommand(gymId);

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

        var command = new ApproveGymCommand(gymId);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(GymErrors.NotFound.Code);
    }
}
