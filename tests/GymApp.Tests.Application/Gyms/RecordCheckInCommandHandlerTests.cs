using GymApp.Modules.Identity.Shared;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Application.Commands.RecordCheckIn;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.Errors;
using GymApp.Modules.Gyms.Domain.Repositories;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Tests.Application.Gyms;

public class RecordCheckInCommandHandlerTests
{
    private readonly IGymRepository _gymRepository = Substitute.For<IGymRepository>();
    private readonly ICheckInRepository _checkInRepository = Substitute.For<ICheckInRepository>();
    private readonly IGymMemberDirectory _memberDirectory = Substitute.For<IGymMemberDirectory>();
    private readonly IGymMemberRepository _gymMemberRepository = Substitute.For<IGymMemberRepository>();
    private readonly IGymsUnitOfWork _unitOfWork = Substitute.For<IGymsUnitOfWork>();
    private readonly IUserProfileLookup _userProfileLookup = Substitute.For<IUserProfileLookup>();
    private readonly RecordCheckInCommandHandler _sut;

    public RecordCheckInCommandHandlerTests()
    {
        _gymMemberRepository.GetAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((GymMember?)null);
        _userProfileLookup.GetProfileAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((UserProfileInfo?)null);
        _sut = new RecordCheckInCommandHandler(
            _gymRepository,
            _checkInRepository,
            new CheckInEligibilityChecker(_memberDirectory, _gymMemberRepository, _checkInRepository),
            _userProfileLookup,
            _unitOfWork);
    }

    private static Gym CreateActiveGym()
    {
        var address = new Address("123 Main St", "Cairo", "Cairo", "Egypt", "12345", null, null);
        var contact = new ContactInfo("gym@example.com", "+201234567890", null);
        var gymCode = GymCode.Generate();
        var gym = Gym.Create(
            Guid.NewGuid(),
            new GymDetails
            {
                Name = "Test Gym",
                Description = "Description",
                Address = address,
                ContactInfo = contact,
                GymCode = gymCode
            });
        gym.Approve();
        return gym;
    }

    private void GivenActiveGym(Guid gymId) =>
        _gymRepository.GetByIdAsync(gymId, Arg.Any<CancellationToken>()).Returns(CreateActiveGym());

    private async Task AssertHandleFailsWith(RecordCheckInCommand command, GymApp.Common.Domain.Error expected)
    {
        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(expected.Code);
    }

    [Fact]
    public async Task Handle_ValidCheckIn_ReturnsSuccess()
    {
        // Arrange
        var gymId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        GivenActiveGym(gymId);
        _checkInRepository.GetLatestForMemberAsync(gymId, memberId, Arg.Any<CancellationToken>())
            .Returns((CheckIn?)null);

        var command = new RecordCheckInCommand(gymId, "QR", null, null, memberId, null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.CheckInId.Should().NotBeNull();
        await _checkInRepository.Received(1).AddAsync(Arg.Any<CheckIn>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GymNotFound_ReturnsFailure()
    {
        // Arrange
        var gymId = Guid.NewGuid();
        _gymRepository.GetByIdAsync(gymId, Arg.Any<CancellationToken>())
            .Returns((Gym?)null);

        var command = new RecordCheckInCommand(gymId, "QR", null, null, Guid.NewGuid(), null);

        // Act + Assert
        await AssertHandleFailsWith(command, GymErrors.NotFound);
    }

    [Fact]
    public async Task Handle_NoMemberIdProvided_ReturnsInvalidCheckIn()
    {
        // Arrange
        var gymId = Guid.NewGuid();
        GivenActiveGym(gymId);

        var command = new RecordCheckInCommand(gymId, "QR", null, null, null, null);

        // Act + Assert
        await AssertHandleFailsWith(command, GymErrors.InvalidCheckIn);
    }

    [Fact]
    public async Task Handle_QrTokenResolvesToMember_ReturnsSuccess()
    {
        var gymId = Guid.NewGuid();
        var resolvedMemberId = Guid.NewGuid();
        GivenActiveGym(gymId);
        _memberDirectory.FindMemberIdByNumberAsync(gymId, "M-42", Arg.Any<CancellationToken>())
            .Returns(resolvedMemberId);
        _checkInRepository.GetLatestForMemberAsync(gymId, resolvedMemberId, Arg.Any<CancellationToken>())
            .Returns((CheckIn?)null);

        var command = new RecordCheckInCommand(gymId, "QR", "M-42", null, null, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _memberDirectory.Received(1).FindMemberIdByNumberAsync(gymId, "M-42", Arg.Any<CancellationToken>());
        await _checkInRepository.Received(1).AddAsync(
            Arg.Is<CheckIn>(c => c.MemberId == resolvedMemberId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_QrTokenUnresolvable_ReturnsInvalidCheckIn()
    {
        var gymId = Guid.NewGuid();
        GivenActiveGym(gymId);
        _memberDirectory.FindMemberIdByNumberAsync(gymId, "UNKNOWN", Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var command = new RecordCheckInCommand(gymId, "QR", "UNKNOWN", null, null, null);

        await AssertHandleFailsWith(command, GymErrors.InvalidCheckIn);
    }

    [Fact]
    public async Task Handle_DuplicateCheckInToday_ReturnsFailure()
    {
        // Arrange
        var gymId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        GivenActiveGym(gymId);

        var existingCheckIn = CheckIn.Create(gymId, memberId, CheckInMethod.QR);
        _checkInRepository.GetLatestForMemberAsync(gymId, memberId, Arg.Any<CancellationToken>())
            .Returns(existingCheckIn);

        var command = new RecordCheckInCommand(gymId, "QR", null, null, memberId, null);

        // Act + Assert
        await AssertHandleFailsWith(command, GymErrors.DuplicateCheckIn);
    }
}
