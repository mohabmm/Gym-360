using GymApp.Common.Application.Tenancy;
using GymApp.Modules.Identity.Application.Abstractions;
using GymApp.Modules.Identity.Application.Commands.RefreshToken;
using GymApp.Modules.Identity.Application.Commands.SelectGym;
using GymApp.Modules.Identity.Application.Contracts;
using GymApp.Modules.Identity.Domain.Entities;
using GymApp.Modules.Identity.Domain.Enums;
using GymApp.Modules.Identity.Domain.Repositories;
using GymApp.Modules.Identity.Domain.ValueObjects;
using DomainRefreshToken = GymApp.Modules.Identity.Domain.Entities.RefreshToken;

namespace GymApp.Tests.Application.Identity;

/// <summary>
/// An owner's session stays on the gym they picked: select-gym saves its refresh token with the
/// gym, and refresh re-issues a token for that gym while they still own it.
/// </summary>
public sealed class OwnerGymSessionTests
{
    private readonly Guid _ownerAggregateId = Guid.NewGuid();
    private readonly GymRegistration _gym = new() { GymId = Guid.NewGuid(), Name = "Gold's Cairo", Slug = "golds", SchemaPrefix = "g7" };

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IIdentityUnitOfWork _unitOfWork = Substitute.For<IIdentityUnitOfWork>();
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();
    private readonly IGymRegistry _registry = Substitute.For<IGymRegistry>();
    private readonly User _owner;

    public OwnerGymSessionTests()
    {
        _owner = User.Create(new NewUserDetails
        {
            Email = Email.Create("owner@example.com").Value,
            PhoneNumber = PhoneNumber.Create("+201234567890").Value,
            PasswordHash = "hashed",
            FirstName = "Mona",
            LastName = "Adel",
            Gender = Gender.Female,
        }, UserRole.GymOwner);
        _owner.LinkToOwner(_ownerAggregateId);
        _gym.OwnerId = _ownerAggregateId;

        _users.GetByIdAsync(_owner.Id, Arg.Any<CancellationToken>()).Returns(_owner);
        _registry.FindAsync(_gym.GymId, Arg.Any<CancellationToken>()).Returns(_gym);
        _jwt.GenerateRefreshTokenAsync(Arg.Any<CancellationToken>()).Returns("rt-1", "rt-2");
        _jwt.GetRefreshTokenExpiryTime().Returns(DateTime.UtcNow.AddDays(30));
        _jwt.GenerateAccessTokenForGymAsync(_owner, _gym.GymId, "g7", Arg.Any<CancellationToken>()).Returns("gym-access");
        _jwt.GenerateAccessTokenAsync(_owner, Arg.Any<CancellationToken>()).Returns("no-gym-access");
    }

    private SelectGymCommandHandler SelectGym() => new(_users, _unitOfWork, _jwt, _registry);
    private RefreshTokenCommandHandler Refresh() => new(_users, _unitOfWork, _jwt, _registry);

    private void RefreshTokenBelongsToOwner(string token) =>
        _users.GetByRefreshTokenAsync(token, Arg.Any<CancellationToken>()).Returns(_owner);

    [Fact]
    public async Task Select_gym_saves_its_refresh_token_with_the_gym()
    {
        var result = await SelectGym().Handle(new SelectGymCommand(_owner.Id, _gym.GymId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.RefreshToken.Should().Be("rt-1");
        var saved = _owner.RefreshTokens.Should().ContainSingle().Subject;
        saved.Token.Should().Be("rt-1");
        saved.SelectedGymId.Should().Be(_gym.GymId);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refresh_after_select_gym_keeps_the_owner_on_that_gym()
    {
        await SelectGym().Handle(new SelectGymCommand(_owner.Id, _gym.GymId), default);
        RefreshTokenBelongsToOwner("rt-1");

        var result = await Refresh().Handle(new RefreshTokenCommand("rt-1"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("gym-access");
        result.Value.User.GymId.Should().Be(_gym.GymId);
        result.Value.User.GymName.Should().Be("Gold's Cairo");
        _owner.RefreshTokens.Single(t => t.Token == "rt-2").SelectedGymId.Should().Be(_gym.GymId);
        _owner.RefreshTokens.Single(t => t.Token == "rt-1").IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Refresh_drops_a_gym_the_owner_no_longer_owns()
    {
        _owner.AddRefreshToken(DomainRefreshToken.Create("rt-old", DateTime.UtcNow.AddDays(1), _gym.GymId));
        _gym.OwnerId = Guid.NewGuid(); // gym handed over to someone else
        RefreshTokenBelongsToOwner("rt-old");

        var result = await Refresh().Handle(new RefreshTokenCommand("rt-old"), default);

        result.Value.AccessToken.Should().Be("no-gym-access");
        result.Value.User.GymId.Should().BeNull();
        _owner.RefreshTokens.Single(t => t.Token == "rt-1").SelectedGymId.Should().BeNull();
    }

    [Fact]
    public async Task Refresh_without_a_selected_gym_works_as_before()
    {
        _owner.AddRefreshToken(DomainRefreshToken.Create("rt-login", DateTime.UtcNow.AddDays(1)));
        RefreshTokenBelongsToOwner("rt-login");

        var result = await Refresh().Handle(new RefreshTokenCommand("rt-login"), default);

        result.Value.AccessToken.Should().Be("no-gym-access");
        await _registry.DidNotReceiveWithAnyArgs().FindAsync(default, default);
    }

    [Fact]
    public async Task Select_gym_refuses_a_gym_the_owner_does_not_own()
    {
        _gym.OwnerId = Guid.NewGuid();

        var result = await SelectGym().Handle(new SelectGymCommand(_owner.Id, _gym.GymId), default);

        result.Error.Code.Should().Be("SelectGym.Forbidden");
        _owner.RefreshTokens.Should().BeEmpty();
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}
