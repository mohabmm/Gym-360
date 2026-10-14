using GymApp.Modules.Billing.Shared;
using GymApp.Common.Application.Errors;
using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Application.Commands.LogWorkout;
using GymApp.Modules.Training.Application.DTOs;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;
using GymApp.Modules.Training.Application.Abstractions;
using GymApp.Modules.Training.IntegrationEvents;

namespace GymApp.Tests.Application.Training;

public class LogWorkoutCommandHandlerTests
{
    private readonly IWorkoutLogRepository _workoutLogRepository = Substitute.For<IWorkoutLogRepository>();
    private readonly ITrainingUnitOfWork _unitOfWork = Substitute.For<ITrainingUnitOfWork>();
    private readonly IGymPlanService _gymPlanService = Substitute.For<IGymPlanService>();
    private readonly IIntegrationEventPublisher _integrationEvents = Substitute.For<IIntegrationEventPublisher>();
    private readonly LogWorkoutCommandHandler _sut;

    public LogWorkoutCommandHandlerTests()
    {
        _sut = new LogWorkoutCommandHandler(_workoutLogRepository, _unitOfWork, _gymPlanService, _integrationEvents);
    }

    private static LogWorkoutCommand CreateValidCommand(Guid? gymId = null) =>
        new(
            UserId: Guid.NewGuid(),
            GymId: gymId ?? Guid.NewGuid(),
            WorkoutId: Guid.NewGuid(),
            Date: DateOnly.FromDateTime(DateTime.UtcNow),
            StartedAt: DateTime.UtcNow.AddHours(-1),
            CompletedAt: DateTime.UtcNow,
            DurationMinutes: 60,
            Notes: "Good session",
            Sets: new List<LogSetDto>
            {
                new(Guid.NewGuid(), 1, 10, 60m, "Kg", null, true, null)
            },
            PerceivedExertion: 7);

    [Fact]
    public async Task Handle_FeatureEnabled_LogsWorkout()
    {
        // Arrange
        var command = CreateValidCommand();
        _gymPlanService.IsFeatureEnabledAsync(command.GymId, "session_logging", Arg.Any<CancellationToken>())
            .Returns(true);
        _workoutLogRepository.GetByUserAndDateAsync(
            command.UserId, command.Date, command.WorkoutId, Arg.Any<CancellationToken>())
            .Returns((WorkoutLog?)null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _workoutLogRepository.Received(1).AddAsync(Arg.Any<WorkoutLog>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FeatureDisabled_ReturnsFeatureNotAvailable()
    {
        // Arrange
        var command = CreateValidCommand();
        _gymPlanService.IsFeatureEnabledAsync(command.GymId, "session_logging", Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PLAN.FEATURE_NOT_AVAILABLE");
        await _workoutLogRepository.DidNotReceive().AddAsync(Arg.Any<WorkoutLog>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateLog_ReturnsExistingId()
    {
        // Arrange
        var command = CreateValidCommand();
        _gymPlanService.IsFeatureEnabledAsync(command.GymId, "session_logging", Arg.Any<CancellationToken>())
            .Returns(true);

        var existingLog = WorkoutLog.Create(
            command.UserId, command.GymId, command.Date, command.StartedAt,
            command.WorkoutId, "Existing log");
        _workoutLogRepository.GetByUserAndDateAsync(
            command.UserId, command.Date, command.WorkoutId, Arg.Any<CancellationToken>())
            .Returns(existingLog);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert — POST is idempotent on (UserId, Date, WorkoutId): the existing
        // log's id is returned and no new record is written.
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existingLog.Id);
        await _workoutLogRepository.DidNotReceive().AddAsync(Arg.Any<WorkoutLog>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewLog_EnqueuesWorkoutLoggedForTheWorkoutDate()
    {
        // Arrange — the date is the member's local workout day, which can differ
        // from the UTC timestamp late in the evening in Cairo.
        var command = CreateValidCommand() with { Date = new DateOnly(2026, 10, 14) };
        _gymPlanService.IsFeatureEnabledAsync(command.GymId, "session_logging", Arg.Any<CancellationToken>())
            .Returns(true);
        _workoutLogRepository.GetByUserAndDateAsync(
            command.UserId, command.Date, command.WorkoutId, Arg.Any<CancellationToken>())
            .Returns((WorkoutLog?)null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert — Progress updates the workout streak off this event.
        result.IsSuccess.Should().BeTrue();
        _integrationEvents.Received(1).Enqueue(Arg.Is<WorkoutLoggedIntegrationEvent>(e =>
            e.UserId == command.UserId &&
            e.GymId == command.GymId &&
            DateOnly.FromDateTime(e.LoggedAt) == command.Date));
    }

    [Fact]
    public async Task Handle_DuplicateLog_DoesNotEnqueueWorkoutLogged()
    {
        // Arrange
        var command = CreateValidCommand();
        _gymPlanService.IsFeatureEnabledAsync(command.GymId, "session_logging", Arg.Any<CancellationToken>())
            .Returns(true);
        _workoutLogRepository.GetByUserAndDateAsync(
            command.UserId, command.Date, command.WorkoutId, Arg.Any<CancellationToken>())
            .Returns(WorkoutLog.Create(
                command.UserId, command.GymId, command.Date, command.StartedAt, command.WorkoutId, null));

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert — a retried POST must not count the same workout twice.
        _integrationEvents.DidNotReceive().Enqueue(Arg.Any<IIntegrationEvent>());
    }
}
