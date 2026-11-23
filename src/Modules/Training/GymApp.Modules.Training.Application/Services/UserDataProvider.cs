using GymApp.Common.Application.Abstractions;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.Services;

internal sealed class UserDataProvider : IUserDataProvider
{
    private readonly IWorkoutLogRepository _workoutLogRepository;
    private readonly IWorkoutAssignmentRepository _workoutAssignmentRepository;
    private readonly ITrainingPlanRepository _trainingPlanRepository;
    private readonly IPlanCompletionRepository _planCompletionRepository;

    public UserDataProvider(
        IWorkoutLogRepository workoutLogRepository,
        IWorkoutAssignmentRepository workoutAssignmentRepository,
        ITrainingPlanRepository trainingPlanRepository,
        IPlanCompletionRepository planCompletionRepository)
    {
        _trainingPlanRepository = trainingPlanRepository;
        _planCompletionRepository = planCompletionRepository;
        _workoutLogRepository = workoutLogRepository;
        _workoutAssignmentRepository = workoutAssignmentRepository;
    }

    public string ModuleName => "Training";

    public async Task<object> GetUserDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var recentLogs = await _workoutLogRepository.GetByUserAsync(
            userId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2)),
            DateOnly.FromDateTime(DateTime.UtcNow),
            cancellationToken);

        var plans = await _trainingPlanRepository.ListAllForTraineeAsync(userId, cancellationToken);
        var planWorkouts = await _planCompletionRepository.ListAllForTraineeAsync(userId, cancellationToken);

        return new
        {
            TrainingPlans = plans.Select(p => new
            {
                p.Id,
                p.GymId,
                p.TrainerId,
                p.Name,
                p.NameAr,
                Goal = p.Goal.ToString(),
                p.GoalText,
                p.StartDate,
                Status = p.Status.ToString(),
                p.Document
            }),
            PlanWorkouts = planWorkouts.Select(c => new
            {
                c.Id,
                c.PlanId,
                c.GymId,
                c.DayTitle,
                c.DayTitleAr,
                c.Week,
                c.CompletedAt,
                Source = c.Source.ToString(),
                c.DurationMinutes,
                c.Note,
                c.IsVoided,
                c.Sets
            }),
            WorkoutLogs = recentLogs.Select(l => new
            {
                l.Id,
                l.Date,
                l.WorkoutId,
                l.GymId,
                l.DurationMinutes,
                l.PerceivedExertion,
                l.Notes,
                SetCount = l.LogSets.Count
            })
        };
    }
}
