using FluentAssertions;
using GymApp.Modules.Billing.Shared;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Application.Commands.StartDataImport;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using NSubstitute;

namespace GymApp.Tests.Application.Gyms;

public class StartDataImportCommandHandlerTests
{
    private readonly IGymRepository _gymRepository = Substitute.For<IGymRepository>();
    private readonly IDataImportJobRepository _jobRepository = Substitute.For<IDataImportJobRepository>();
    private readonly IImportFileStorage _fileStorage = Substitute.For<IImportFileStorage>();
    private readonly IGymPlanService _planService = Substitute.For<IGymPlanService>();
    private readonly IImportJobScheduler _scheduler = Substitute.For<IImportJobScheduler>();
    private readonly IGymsUnitOfWork _unitOfWork = Substitute.For<IGymsUnitOfWork>();
    private readonly StartDataImportCommandHandler _sut;

    public StartDataImportCommandHandlerTests()
    {
        _sut = new StartDataImportCommandHandler(
            _planService, _gymRepository, new DataImportJobLauncher(_jobRepository, _fileStorage, _scheduler, _unitOfWork));
    }

    private static Gym CreateGym()
    {
        var address = new Address("123 Main St", "Cairo", "Cairo", "Egypt", "12345", null, null);
        var contact = new ContactInfo("g@x.com", "+201234567890", null);
        return Gym.Create(
            Guid.NewGuid(),
            new GymDetails
            {
                Name = "Test",
                Description = "desc",
                Address = address,
                ContactInfo = contact,
                GymCode = GymCode.Generate()
            });
    }

    private StartDataImportCommand BuildCommand(Guid gymId) => new(
        GymId: gymId,
        UploadedByUserId: Guid.NewGuid(),
        Source: DataImportSource.Excel,
        OriginalFileName: "roster.xlsx",
        FileSizeBytes: 1024,
        Content: new MemoryStream(new byte[] { 1, 2, 3 }));

    [Fact]
    public async Task Handle_feature_disabled_returns_failure()
    {
        var gymId = Guid.NewGuid();
        _planService.IsFeatureEnabledAsync(gymId, "data_import", Arg.Any<CancellationToken>()).Returns(false);
        _planService.GetPlanCodeAsync(gymId, Arg.Any<CancellationToken>()).Returns("starter");

        var result = await _sut.Handle(BuildCommand(gymId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("PLAN.FEATURE_NOT_AVAILABLE");
        await _jobRepository.DidNotReceive().AddAsync(Arg.Any<DataImportJob>(), Arg.Any<CancellationToken>());
        await _scheduler.DidNotReceive().EnqueueAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_feature_enabled_creates_job_and_schedules()
    {
        var gym = CreateGym();
        _planService.IsFeatureEnabledAsync(gym.Id, "data_import", Arg.Any<CancellationToken>()).Returns(true);
        _gymRepository.GetByIdAsync(gym.Id, Arg.Any<CancellationToken>()).Returns(gym);
        _fileStorage.SaveUploadAsync(Arg.Is<ImportUpload>(u => u.GymId == gym.Id), Arg.Any<CancellationToken>())
            .Returns("gym/uploads/x.xlsx");

        var result = await _sut.Handle(BuildCommand(gym.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _jobRepository.Received(1).AddAsync(Arg.Any<DataImportJob>(), Arg.Any<CancellationToken>());
        await _scheduler.Received(1).EnqueueAsync(result.Value, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_unknown_gym_returns_not_found()
    {
        var gymId = Guid.NewGuid();
        _planService.IsFeatureEnabledAsync(gymId, "data_import", Arg.Any<CancellationToken>()).Returns(true);
        _gymRepository.GetByIdAsync(gymId, Arg.Any<CancellationToken>()).Returns((Gym?)null);

        var result = await _sut.Handle(BuildCommand(gymId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Gyms.NotFound");
    }
}
