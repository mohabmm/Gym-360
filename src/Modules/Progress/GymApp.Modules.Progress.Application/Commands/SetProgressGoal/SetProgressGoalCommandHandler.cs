using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Billing.Shared;
using GymApp.Modules.Progress.Application.Abstractions;
using GymApp.Modules.Progress.Application.DTOs;
using GymApp.Modules.Progress.Application.Mappers;
using GymApp.Modules.Progress.Application.Services;
using GymApp.Modules.Progress.Domain;
using GymApp.Modules.Progress.Domain.Entities;
using GymApp.Modules.Progress.Domain.Enums;
using GymApp.Modules.Progress.Domain.Repositories;

namespace GymApp.Modules.Progress.Application.Commands.SetProgressGoal;

internal sealed class SetProgressGoalCommandHandler : ICommandHandler<SetProgressGoalCommand, ProgressGoalDto>
{
    private readonly IProgressGoalRepository _goals;
    private readonly IProgressUnitOfWork _unitOfWork;
    private readonly IGymPlanService _planService;
    private readonly ITrainerAssignmentLookup _assignments;

    public SetProgressGoalCommandHandler(
        IProgressGoalRepository goals,
        IProgressUnitOfWork unitOfWork,
        IGymPlanService planService,
        ITrainerAssignmentLookup assignments)
    {
        _goals = goals;
        _unitOfWork = unitOfWork;
        _planService = planService;
        _assignments = assignments;
    }

    public async Task<Result<ProgressGoalDto>> Handle(SetProgressGoalCommand request, CancellationToken cancellationToken)
    {
        var subject = new ProgressSubject(request.MemberId, request.GymId, request.TrainerId);
        var denied = await ProgressAccess.CheckAsync(_planService, _assignments, subject, cancellationToken);
        if (denied != Error.None)
            return Result.Failure<ProgressGoalDto>(denied);

        if (!Enum.TryParse<ProgressGoalType>(request.Goal, true, out var goalType) || !Enum.IsDefined(goalType))
            return Result.Failure<ProgressGoalDto>(ProgressErrors.InvalidGoal);

        var choice = new ProgressGoalChoice(goalType, request.TargetWeight, request.TrainerId ?? request.MemberId);
        var existing = await _goals.GetAsync(request.MemberId, request.GymId, cancellationToken);
        var saved = existing is null
            ? await AddAsync(request, choice, cancellationToken)
            : Apply(existing, choice);

        if (saved.IsFailure)
            return Result.Failure<ProgressGoalDto>(saved.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(ProgressMapper.ToDto(saved.Value));
    }

    private async Task<Result<ProgressGoal>> AddAsync(
        SetProgressGoalCommand request, ProgressGoalChoice choice, CancellationToken cancellationToken)
    {
        var created = ProgressGoal.Create(request.MemberId, request.GymId, choice);
        if (created.IsSuccess)
            await _goals.AddAsync(created.Value, cancellationToken);
        return created;
    }

    private static Result<ProgressGoal> Apply(ProgressGoal goal, ProgressGoalChoice choice)
    {
        var changed = goal.Change(choice);
        return changed.IsSuccess ? Result.Success(goal) : Result.Failure<ProgressGoal>(changed.Error);
    }
}
