using GymApp.Modules.Billing.Shared;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Application.Commands.AddTrainer;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using MediatR;

namespace GymApp.Tests.Application.Gyms;

public class AddTrainerCommandHandlerTests
{
    private readonly IGymRepository _gymRepository = Substitute.For<IGymRepository>();
    private readonly IGymPlanService _planService = Substitute.For<IGymPlanService>();
    private readonly IGymQueryService _queryService = Substitute.For<IGymQueryService>();
    private readonly IGymTrainerRequestRepository _trainerRequestRepository = Substitute.For<IGymTrainerRequestRepository>();
    private readonly IGymsUnitOfWork _unitOfWork = Substitute.For<IGymsUnitOfWork>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly AddTrainerCommandHandler _sut;

    public AddTrainerCommandHandlerTests()
    {
        _planService.GetTrainerLimitAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(0);
        _queryService.GetTrainerCountAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(0);
        _trainerRequestRepository.GetAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((GymTrainerRequest?)null);
        _sut = new AddTrainerCommandHandler(
            _gymRepository, _planService, _queryService, _trainerRequestRepository, _unitOfWork, _publisher);
    }

    private static Gym CreateActiveGym(Guid gymId)
    {
        var address = new Address("123 Main St", "Cairo", "Cairo", "Egypt", "12345", null, null);
        var contact = new ContactInfo("gym@example.com", "+201234567890", null);
        var gym = Gym.Create(gymId, "Test Gym", "Description", address, contact, GymCode.Generate());
        gym.Approve();
        return gym;
    }

    [Fact]
    public async Task Handle_WithBioAndSpecializations_PersistsThemOnNewRequest()
    {
        var gymId = Guid.NewGuid();
        var trainerId = Guid.NewGuid();
        _gymRepository.GetByIdAsync(gymId, Arg.Any<CancellationToken>()).Returns(CreateActiveGym(gymId));

        GymTrainerRequest? captured = null;
        await _trainerRequestRepository.AddAsync(
            Arg.Do<GymTrainerRequest>(r => captured = r), Arg.Any<CancellationToken>());

        var command = new AddTrainerCommand(
            gymId, trainerId,
            Bio: "  Strength coach. 10 years.  ",
            Specializations: new[] { "Strength", "strength", " HIIT ", "" });

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.Bio.Should().Be("Strength coach. 10 years.");
        captured.Specializations.Should().BeEquivalentTo(new[] { "Strength", "HIIT" });
        captured.Status.Should().Be(TrainerRequestStatus.Approved);
    }

    [Fact]
    public async Task Handle_WithoutProfile_DoesNotInvokeUpdateProfile()
    {
        var gymId = Guid.NewGuid();
        var trainerId = Guid.NewGuid();
        _gymRepository.GetByIdAsync(gymId, Arg.Any<CancellationToken>()).Returns(CreateActiveGym(gymId));

        GymTrainerRequest? captured = null;
        await _trainerRequestRepository.AddAsync(
            Arg.Do<GymTrainerRequest>(r => captured = r), Arg.Any<CancellationToken>());

        var command = new AddTrainerCommand(gymId, trainerId);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        captured!.Bio.Should().BeNull();
        captured.Specializations.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_PendingRequestExists_ApprovesAndUpdatesProfile()
    {
        var gymId = Guid.NewGuid();
        var trainerId = Guid.NewGuid();
        _gymRepository.GetByIdAsync(gymId, Arg.Any<CancellationToken>()).Returns(CreateActiveGym(gymId));

        var existing = GymTrainerRequest.CreatePending(gymId, trainerId);
        _trainerRequestRepository
            .GetAsync(gymId, trainerId, Arg.Any<CancellationToken>())
            .Returns(existing);

        var command = new AddTrainerCommand(
            gymId, trainerId,
            Bio: "Yoga.",
            Specializations: new[] { "Yoga" });

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.Status.Should().Be(TrainerRequestStatus.Approved);
        existing.Bio.Should().Be("Yoga.");
        existing.Specializations.Should().ContainSingle().Which.Should().Be("Yoga");
        await _trainerRequestRepository.DidNotReceive().AddAsync(Arg.Any<GymTrainerRequest>(), Arg.Any<CancellationToken>());
        _trainerRequestRepository.Received(1).Update(existing);
    }

    [Fact]
    public async Task Handle_BioOnly_PreservesExistingSpecializations()
    {
        var gymId = Guid.NewGuid();
        var trainerId = Guid.NewGuid();
        _gymRepository.GetByIdAsync(gymId, Arg.Any<CancellationToken>()).Returns(CreateActiveGym(gymId));

        var existing = GymTrainerRequest.CreateApproved(gymId, trainerId);
        existing.UpdateProfile(new[] { "Powerlifting" }, "old bio");
        _trainerRequestRepository
            .GetAsync(gymId, trainerId, Arg.Any<CancellationToken>())
            .Returns(existing);

        var command = new AddTrainerCommand(gymId, trainerId, Bio: "new bio");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.Bio.Should().Be("new bio");
        existing.Specializations.Should().ContainSingle().Which.Should().Be("Powerlifting");
    }
}
