using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Identity.Shared;
using GymApp.Modules.Training.Application.Abstractions;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Errors;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.TrainingPlans.Challenge;

/// <summary>The desk's "earned the reward" list and its "reward delivered" tick.</summary>
internal sealed class ChallengeAwardsHandler
    : IQueryHandler<ListChallengeAwardsQuery, IReadOnlyList<ChallengeAwardDto>>,
      ICommandHandler<SetChallengeAwardDeliveredCommand, ChallengeAwardDto>
{
    private readonly ChallengeAccess _access;
    private readonly IChallengeAwardRepository _awards;
    private readonly IUserDisplayLookup _users;
    private readonly ITrainingUnitOfWork _unitOfWork;

    public ChallengeAwardsHandler(
        ChallengeAccess access, IChallengeAwardRepository awards, IUserDisplayLookup users, ITrainingUnitOfWork unitOfWork)
    {
        _access = access;
        _awards = awards;
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<ChallengeAwardDto>>> Handle(ListChallengeAwardsQuery request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureEnabledAsync(request.Actor.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<IReadOnlyList<ChallengeAwardDto>>(enabled.Error);

        var awards = await _awards.ListAsync(request.Actor.GymId, request.Delivered, cancellationToken);
        var names = await NamesAsync(awards, cancellationToken);
        return Result.Success<IReadOnlyList<ChallengeAwardDto>>(awards.Select(a => ToDto(a, names)).ToList());
    }

    public async Task<Result<ChallengeAwardDto>> Handle(SetChallengeAwardDeliveredCommand request, CancellationToken cancellationToken)
    {
        var enabled = await _access.EnsureEnabledAsync(request.Actor.GymId, cancellationToken);
        if (enabled.IsFailure)
            return Result.Failure<ChallengeAwardDto>(enabled.Error);

        var award = await _awards.GetByIdAsync(request.Actor.GymId, request.AwardId, cancellationToken);
        if (award is null)
            return Result.Failure<ChallengeAwardDto>(TrainingPlanErrors.AwardNotFound);
        award.SetDelivered(request.Delivered, request.Actor.UserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(ToDto(award, await NamesAsync(new[] { award }, cancellationToken)));
    }

    private Task<IReadOnlyDictionary<Guid, UserDisplay>> NamesAsync(IReadOnlyCollection<ChallengeAward> awards, CancellationToken cancellationToken)
    {
        var ids = awards
            .SelectMany(a => new[] { a.TraineeId, a.TrainerId, a.DeliveredBy })
            .OfType<Guid>()
            .Distinct()
            .ToList();
        return _users.GetDisplayNamesAsync(ids, cancellationToken);
    }

    private static string? NameOf(IReadOnlyDictionary<Guid, UserDisplay> names, Guid? userId) =>
        userId is { } id && names.TryGetValue(id, out var user) && !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : null;

    private static ChallengeAwardDto ToDto(ChallengeAward a, IReadOnlyDictionary<Guid, UserDisplay> names) => new()
    {
        AwardId = a.Id,
        TraineeId = a.TraineeId,
        TraineeName = NameOf(names, a.TraineeId) ?? "—",
        TrainerId = a.TrainerId,
        TrainerName = NameOf(names, a.TrainerId),
        Count = a.CountAtReach,
        Target = a.Target,
        WindowStart = a.WindowStart,
        WindowEnd = a.WindowEnd,
        ReachedAt = a.ReachedAt,
        RewardTextAr = a.RewardTextAr,
        RewardTextEn = a.RewardTextEn,
        DeliveredAt = a.DeliveredAt,
        DeliveredByName = NameOf(names, a.DeliveredBy)
    };
}
