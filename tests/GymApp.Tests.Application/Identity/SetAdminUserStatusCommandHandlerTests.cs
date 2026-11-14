using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Application.Commands.AdminUsers;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Repositories;

namespace GymApp.Tests.Application.Identity;

public class SetAdminUserStatusCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IIdentityUnitOfWork _unitOfWork = Substitute.For<IIdentityUnitOfWork>();
    private readonly IUserLockoutCache _lockoutCache = Substitute.For<IUserLockoutCache>();
    private readonly SetAdminUserStatusCommandHandler _sut;

    public SetAdminUserStatusCommandHandlerTests()
    {
        _sut = new SetAdminUserStatusCommandHandler(_userRepository, _unitOfWork, _lockoutCache);
    }

    private User GivenUser()
    {
        var user = AdminUserManagementTestData.NewUser();
        _userRepository.GetByIdWithRefreshTokensAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    [Fact]
    public async Task Suspend_ActiveUser_SuspendsRevokesTokensAndBlocksImmediately()
    {
        var user = GivenUser();

        var result = await _sut.Handle(new SetAdminUserStatusCommand(user.Id, Guid.NewGuid(), "suspended"), default);

        result.IsSuccess.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Suspended);
        user.RefreshTokens.Should().OnlyContain(t => t.IsRevoked);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _lockoutCache.Received(1).MarkBlockedAsync(user.Id, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Activate_SuspendedUser_ActivatesAndClearsLockout()
    {
        var user = GivenUser();
        user.Suspend();

        var result = await _sut.Handle(new SetAdminUserStatusCommand(user.Id, Guid.NewGuid(), "active"), default);

        result.IsSuccess.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Active);
        await _lockoutCache.Received(1).ClearAsync(user.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Activate_DeletedUser_ReturnsConflict()
    {
        var user = GivenUser();
        user.MarkDeleted();

        var result = await _sut.Handle(new SetAdminUserStatusCommand(user.Id, Guid.NewGuid(), "active"), default);

        result.Error.Code.Should().Be("AdminUser.CannotReactivateDeleted.Conflict");
        user.Status.Should().Be(UserStatus.Deleted);
    }

    [Fact]
    public async Task OwnAccount_IsRejectedWithoutLoadingTheUser()
    {
        var me = Guid.NewGuid();

        var result = await _sut.Handle(new SetAdminUserStatusCommand(me, me, "suspended"), default);

        result.Error.Code.Should().Be("AdminUser.CannotChangeOwnStatus");
        await _userRepository.DidNotReceiveWithAnyArgs().GetByIdWithRefreshTokensAsync(default);
    }

    [Fact]
    public async Task UnknownUser_ReturnsNotFound()
    {
        var result = await _sut.Handle(new SetAdminUserStatusCommand(Guid.NewGuid(), Guid.NewGuid(), "suspended"), default);

        result.Error.Code.Should().Be("User.NotFound");
    }
}
