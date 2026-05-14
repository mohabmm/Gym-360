using GymApp.Common.Application.Tenancy;
using GymApp.Common.Domain;
using GymApp.Modules.Gyms.Application.Abstractions;
using GymApp.Modules.Gyms.Application.Commands.RegisterGym;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace GymApp.Tests.Application.Gyms;

public class RegisterGymCommandHandlerTests
{
    private readonly IGymRepository _gymRepository = Substitute.For<IGymRepository>();
    private readonly IGymsUnitOfWork _unitOfWork = Substitute.For<IGymsUnitOfWork>();
    private readonly IGymProvisioner _provisioner = Substitute.For<IGymProvisioner>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly RegisterGymCommandHandler _sut;

    public RegisterGymCommandHandlerTests()
    {
        _tenantContext.IsHardIsolation.Returns(false);
        _sut = new RegisterGymCommandHandler(
            _gymRepository, _unitOfWork, _provisioner, _tenantContext, _scopeFactory);
    }

    private static RegisterGymCommand CreateValidCommand() =>
        new(
            OwnerId: Guid.NewGuid(),
            Name: "Test Gym",
            Description: "A great gym",
            Address: new AddressDto("123 Main St", "Cairo", "Cairo", "Egypt", "12345"),
            Contact: new ContactInfoDto("gym@example.com", "+201234567890"));

    [Fact]
    public async Task Handle_ValidCommand_CreatesGymAndReturnsId()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _gymRepository.Received(1).AddAsync(Arg.Any<Gym>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _provisioner.DidNotReceive().ProvisionAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
