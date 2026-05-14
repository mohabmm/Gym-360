using GymApp.Common.Domain;
using GymApp.Modules.Identity.Application.Commands.ExportMyData;
using GymApp.Modules.Identity.Application.Contracts;
using GymApp.Modules.Identity.Application.DTOs;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Domain.ValueObjects;

namespace GymApp.Tests.Application.Identity;

public class ExportMyDataCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IDataExportService _dataExportService = Substitute.For<IDataExportService>();
    private readonly ExportMyDataCommandHandler _sut;

    public ExportMyDataCommandHandlerTests()
    {
        _sut = new ExportMyDataCommandHandler(_userRepository, _dataExportService);
    }

    [Fact]
    public async Task Handle_ExistingUser_ReturnsExportData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = User.Create(
            Email.Create("test@example.com").Value,
            PhoneNumber.Create("+201234567890").Value,
            "hashed", "John", "Doe", Gender.Male, UserRole.Trainee);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var exportResponse = new DataExportResponse("https://storage.example.com/export.zip", DateTime.UtcNow.AddHours(24));
        _dataExportService.ExportUserDataAsync(userId, Arg.Any<CancellationToken>())
            .Returns(exportResponse);

        var command = new ExportMyDataCommand(userId);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DownloadUrl.Should().Be("https://storage.example.com/export.zip");
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var command = new ExportMyDataCommand(userId);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.NotFound");
    }
}
