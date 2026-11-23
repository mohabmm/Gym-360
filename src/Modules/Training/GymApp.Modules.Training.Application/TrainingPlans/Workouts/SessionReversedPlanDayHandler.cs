using GymApp.Modules.Scheduling.IntegrationEvents;
using GymApp.Modules.Training.Application.Abstractions;
using GymApp.Modules.Training.Domain.Repositories;
using MediatR;

namespace GymApp.Modules.Training.Application.TrainingPlans.Workouts;

/// <summary>
/// pt.training_plan, part C: the desk reversed a PT session, so the plan workout it recorded
/// stops counting. A first-month challenge award already given is not taken back.
/// </summary>
internal sealed class SessionReversedPlanDayHandler : INotificationHandler<SessionReversedIntegrationEvent>
{
    private readonly IPlanCompletionRepository _completions;
    private readonly ITrainingUnitOfWork _unitOfWork;

    public SessionReversedPlanDayHandler(IPlanCompletionRepository completions, ITrainingUnitOfWork unitOfWork)
    {
        _completions = completions;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SessionReversedIntegrationEvent e, CancellationToken cancellationToken)
    {
        var linked = await _completions.ListForSessionAsync(e.GymId, e.SessionId, cancellationToken);
        if (linked.Count == 0)
            return;
        foreach (var completion in linked)
            completion.Void();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
