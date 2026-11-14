using GymApp.Common.Application.Tenancy;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Application.Commands.AdminUsers;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Repositories;

namespace GymApp.Tests.Application.Identity;

public class DeleteAdminUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IIdentityUnitOfWork _unitOfWork = Substitute.For<IIdentityUnitOfWork>();
    private readonly IGymRegistry _gymRegistry = Substitute.For<IGymRegistry>();
    private readonly IUserLockoutCache _lockoutCache = Substitute.For<IUserLockoutCache>();
    private readonly IUserDataPurger _purger = Substitute.For<IUserDataPurger>();
    private readonly DeleteAdminUserCommandHandler _sut;

    public DeleteAdminUserCommandHandlerTests()
    {
        _gymRegistry.GetByOwnerAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<GymRegistration>());
        _sut = new DeleteAdminUserCommandHandler(_userRepository, _unitOfWork, _gymRegistry, _lockoutCache, _purger);
    }

    private User GivenUser(UserRole role = UserRole.Trainee)
    {
        var user = AdminUserManagementTestData.NewUser(role);
        _userRepository.GetByIdWithRefreshTokensAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    [Fact]
    public async Task Trainee_IsLockedOutThenErasedThenRemoved_InThatOrder()
    {
        var user = GivenUser();

        var result = await _sut.Handle(new DeleteAdminUserCommand(user.Id, Guid.NewGuid()), default);

        result.IsSuccess.Should().BeTrue();
        user.RefreshTokens.Should().OnlyContain(t => t.IsRevoked);
        Received.InOrder(() =>
        {
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            _lockoutCache.MarkDeletedAsync(user.Id, Arg.Any<CancellationToken>());
            _purger.PurgeAsync(user.Id, Arg.Any<CancellationToken>());
            _userRepository.DeleteSatelliteRecordsAsync(user.Id, Arg.Any<CancellationToken>());
            _userRepository.Remove(user);
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task OwnerWithGyms_IsRejectedAndNothingIsTouched()
    {
        var user = GivenUser(UserRole.GymOwner);
        var ownerId = Guid.NewGuid();
        user.LinkToOwner(ownerId);
        _gymRegistry.GetByOwnerAsync(ownerId, Arg.Any<CancellationToken>())
            .Returns(new[] { new GymRegistration { GymId = Guid.NewGuid(), OwnerId = ownerId } });

        var result = await _sut.Handle(new DeleteAdminUserCommand(user.Id, Guid.NewGuid()), default);

        result.Error.Code.Should().Be("AdminUser.OwnerHasGyms.Conflict");
        user.Status.Should().Be(UserStatus.Active);
        _userRepository.DidNotReceive().Remove(Arg.Any<User>());
        await _purger.DidNotReceiveWithAnyArgs().PurgeAsync(default);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public async Task SuperAdmin_IsDeletedOnlyWhenAnotherSuperAdminRemains(int superAdminCount, bool deleted)
    {
        var user = GivenUser(UserRole.PlatformAdmin);
        _userRepository.GetPlatformAdminByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(PlatformAdmin.Create(user.Id, AdminRole.SuperAdmin, null));
        _userRepository.CountSuperAdminsAsync(Arg.Any<CancellationToken>()).Returns(superAdminCount);

        var result = await _sut.Handle(new DeleteAdminUserCommand(user.Id, Guid.NewGuid()), default);

        result.IsSuccess.Should().Be(deleted);
        _userRepository.Received(deleted ? 1 : 0).Remove(user);
        if (!deleted)
            result.Error.Code.Should().Be("AdminUser.CannotDeleteLastSuperAdmin.Conflict");
    }

    [Fact]
    public async Task Self_IsRejected()
    {
        var me = Guid.NewGuid();

        var result = await _sut.Handle(new DeleteAdminUserCommand(me, me), default);

        result.Error.Code.Should().Be("AdminUser.CannotDeleteSelf");
    }
}
