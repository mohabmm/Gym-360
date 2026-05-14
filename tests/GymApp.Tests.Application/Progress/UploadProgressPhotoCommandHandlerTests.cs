using GymApp.Common.Application.Abstractions;
using GymApp.Common.Application.Errors;
using GymApp.Common.Domain;
using GymApp.Modules.Progress.Application.Commands.UploadProgressPhoto;
using GymApp.Modules.Progress.Domain;
using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.Repositories;
using GymApp.Modules.Progress.Application.Abstractions;

namespace GymApp.Tests.Application.Progress;

public class UploadProgressPhotoCommandHandlerTests
{
    private readonly IProgressPhotoRepository _repository = Substitute.For<IProgressPhotoRepository>();
    private readonly IProgressUnitOfWork _unitOfWork = Substitute.For<IProgressUnitOfWork>();
    private readonly IGymPlanService _planService = Substitute.For<IGymPlanService>();
    private readonly UploadProgressPhotoCommandHandler _sut;

    public UploadProgressPhotoCommandHandlerTests()
    {
        _sut = new UploadProgressPhotoCommandHandler(_repository, _unitOfWork, _planService);
    }

    private static UploadProgressPhotoCommand CreateValidCommand(Guid? gymId = null) =>
        new(
            UserId: Guid.NewGuid(),
            GymId: gymId ?? Guid.NewGuid(),
            PhotoUrl: "https://storage.example.com/photo.jpg",
            ThumbnailUrl: "https://storage.example.com/thumb.jpg",
            Angle: "Front",
            Date: DateOnly.FromDateTime(DateTime.UtcNow),
            Notes: "Week 4 progress");

    [Fact]
    public async Task Handle_FeatureEnabled_UploadsPhoto()
    {
        // Arrange
        var command = CreateValidCommand();
        _planService.IsFeatureEnabledAsync(command.GymId, "progress_photos", Arg.Any<CancellationToken>())
            .Returns(true);
        _planService.GetPhotoLimitPerMemberAsync(command.GymId, Arg.Any<CancellationToken>())
            .Returns(50);
        _repository.GetCountAsync(command.UserId, command.GymId, Arg.Any<CancellationToken>())
            .Returns(5);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _repository.Received(1).AddAsync(Arg.Any<ProgressPhoto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FeatureDisabled_ReturnsFeatureNotAvailable()
    {
        // Arrange
        var command = CreateValidCommand();
        _planService.IsFeatureEnabledAsync(command.GymId, "progress_photos", Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PLAN.FEATURE_NOT_AVAILABLE");
        await _repository.DidNotReceive().AddAsync(Arg.Any<ProgressPhoto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PhotoLimitReached_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        _planService.IsFeatureEnabledAsync(command.GymId, "progress_photos", Arg.Any<CancellationToken>())
            .Returns(true);
        _planService.GetPhotoLimitPerMemberAsync(command.GymId, Arg.Any<CancellationToken>())
            .Returns(10);
        _repository.GetCountAsync(command.UserId, command.GymId, Arg.Any<CancellationToken>())
            .Returns(10); // At limit

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ProgressErrors.PhotoLimitReached.Code);
    }

    [Fact]
    public async Task Handle_UnderPhotoLimit_UploadsSuccessfully()
    {
        // Arrange
        var command = CreateValidCommand();
        _planService.IsFeatureEnabledAsync(command.GymId, "progress_photos", Arg.Any<CancellationToken>())
            .Returns(true);
        _planService.GetPhotoLimitPerMemberAsync(command.GymId, Arg.Any<CancellationToken>())
            .Returns(10);
        _repository.GetCountAsync(command.UserId, command.GymId, Arg.Any<CancellationToken>())
            .Returns(9); // One under limit

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
