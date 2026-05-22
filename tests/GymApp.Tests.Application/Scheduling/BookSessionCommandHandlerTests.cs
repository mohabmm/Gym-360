using GymApp.Common.Domain;
using GymApp.Modules.Scheduling.Application.Commands.BookSession;
using GymApp.Modules.Scheduling.Domain.Entities;
using GymApp.Modules.Scheduling.Domain.Enums;
using GymApp.Modules.Scheduling.Domain.Errors;
using GymApp.Modules.Scheduling.Domain.Repositories;
using GymApp.Modules.Scheduling.Application.Abstractions;
using GymApp.Modules.Scheduling.Application.Contracts;

namespace GymApp.Tests.Application.Scheduling;

public class BookSessionCommandHandlerTests
{
    private readonly ISessionRepository _sessionRepository = Substitute.For<ISessionRepository>();
    private readonly ITrainerAvailabilityRepository _availabilityRepository = Substitute.For<ITrainerAvailabilityRepository>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();
    private readonly ISessionPackageService _sessionPackageService = Substitute.For<ISessionPackageService>();
    private readonly BookSessionCommandHandler _sut;

    public BookSessionCommandHandlerTests()
    {
        _sut = new BookSessionCommandHandler(_sessionRepository, _availabilityRepository, _unitOfWork, _sessionPackageService);
    }

    [Fact]
    public async Task Handle_InvalidSessionType_ReturnsFailure()
    {
        // Arrange
        var command = new BookSessionCommand(
            GymId: Guid.NewGuid(),
            TrainerId: Guid.NewGuid(),
            MemberId: Guid.NewGuid(),
            Date: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Time: new TimeOnly(10, 0),
            DurationMinutes: 60,
            SessionType: "InvalidType",
            Focus: "General");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.InvalidSessionType");
    }

    [Fact]
    public async Task Handle_NoTrainerAvailability_ReturnsFailure()
    {
        // Arrange
        var trainerId = Guid.NewGuid();
        var gymId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        _availabilityRepository.GetByTrainerAndGymAsync(trainerId, gymId, Arg.Any<CancellationToken>())
            .Returns((TrainerAvailability?)null);
        // Pass the capacity guard so the availability check is the one under test.
        _sessionPackageService.GetBookableSessionsAsync(memberId, gymId, Arg.Any<CancellationToken>())
            .Returns(1);

        var command = new BookSessionCommand(
            GymId: gymId,
            TrainerId: trainerId,
            MemberId: memberId,
            Date: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Time: new TimeOnly(10, 0),
            DurationMinutes: 60,
            SessionType: "PersonalTraining",
            Focus: "General");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.AvailabilityNotFound");
    }
}
