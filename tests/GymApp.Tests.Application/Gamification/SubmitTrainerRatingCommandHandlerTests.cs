using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Errors;
using GymApp.Common.Domain;
using GymApp.Modules.Gamification.Application.Commands.SubmitTrainerRating;
using GymApp.Modules.Gamification.Domain.Entities;
using GymApp.Modules.Gamification.Domain.Errors;
using GymApp.Modules.Gamification.Domain.Repositories;
using GymApp.Modules.Gamification.Application.Abstractions;

namespace GymApp.Tests.Application.Gamification;

public class SubmitTrainerRatingCommandHandlerTests
{
    private readonly ITrainerRatingRepository _ratingRepository = Substitute.For<ITrainerRatingRepository>();
    private readonly ITrainerPublicProfileRepository _profileRepository = Substitute.For<ITrainerPublicProfileRepository>();
    private readonly IGymPlanService _gymPlanService = Substitute.For<IGymPlanService>();
    private readonly IGamificationUnitOfWork _unitOfWork = Substitute.For<IGamificationUnitOfWork>();
    private readonly SubmitTrainerRatingCommandHandler _sut;

    public SubmitTrainerRatingCommandHandlerTests()
    {
        _sut = new SubmitTrainerRatingCommandHandler(
            _ratingRepository,
            _profileRepository,
            _gymPlanService,
            _unitOfWork);
    }

    private static SubmitTrainerRatingCommand CreateValidCommand(Guid? gymId = null) =>
        new(
            GymId: gymId ?? Guid.NewGuid(),
            TrainerId: Guid.NewGuid(),
            ClientId: Guid.NewGuid(),
            SessionId: Guid.NewGuid(),
            Stars: 5,
            Comment: "Great session!");

    [Fact]
    public async Task Handle_FeatureEnabled_SubmitsRating()
    {
        // Arrange
        var command = CreateValidCommand();
        _gymPlanService.IsFeatureEnabledAsync(command.GymId, "trainer_ratings", Arg.Any<CancellationToken>())
            .Returns(true);
        _gymPlanService.GetPlanCodeAsync(command.GymId, Arg.Any<CancellationToken>())
            .Returns("pro");
        _ratingRepository.GetBySessionAsync(command.SessionId, Arg.Any<CancellationToken>())
            .Returns((TrainerRating?)null);
        _profileRepository.GetByTrainerAndGymAsync(command.TrainerId, command.GymId, Arg.Any<CancellationToken>())
            .Returns((TrainerPublicProfile?)null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _ratingRepository.Received(1).AddAsync(Arg.Any<TrainerRating>(), Arg.Any<CancellationToken>());
        await _profileRepository.Received(1).AddAsync(Arg.Any<TrainerPublicProfile>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FeatureDisabled_ReturnsFeatureNotAvailable()
    {
        // Arrange
        var command = CreateValidCommand();
        _gymPlanService.IsFeatureEnabledAsync(command.GymId, "trainer_ratings", Arg.Any<CancellationToken>())
            .Returns(false);
        _gymPlanService.GetPlanCodeAsync(command.GymId, Arg.Any<CancellationToken>())
            .Returns("free");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PLAN.FEATURE_NOT_AVAILABLE");
        await _ratingRepository.DidNotReceive().AddAsync(Arg.Any<TrainerRating>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyRatedForSession_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        _gymPlanService.IsFeatureEnabledAsync(command.GymId, "trainer_ratings", Arg.Any<CancellationToken>())
            .Returns(true);
        _gymPlanService.GetPlanCodeAsync(command.GymId, Arg.Any<CancellationToken>())
            .Returns("pro");

        var existingRatingResult = TrainerRating.Create(
            command.GymId, command.TrainerId, command.ClientId, command.SessionId, 4, "Good");
        _ratingRepository.GetBySessionAsync(command.SessionId, Arg.Any<CancellationToken>())
            .Returns(existingRatingResult.Value);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(GamificationErrors.AlreadyRatedForSession.Code);
    }
}
