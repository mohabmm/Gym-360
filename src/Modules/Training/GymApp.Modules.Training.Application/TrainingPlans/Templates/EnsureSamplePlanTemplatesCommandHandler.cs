using GymApp.Common.Application.Messaging;
using GymApp.Common.Domain;
using GymApp.Modules.Training.Domain.Entities;
using GymApp.Modules.Training.Domain.Repositories;

namespace GymApp.Modules.Training.Application.TrainingPlans.Templates;

/// <summary>
/// Gives a gym its three sample templates once: skipped when the gym already has (or ever had)
/// a gym template, so an owner who deleted the samples never gets them back.
/// </summary>
internal sealed class EnsureSamplePlanTemplatesCommandHandler : ICommandHandler<EnsureSamplePlanTemplatesCommand, int>
{
    private readonly IPlanTemplateRepository _templates;
    private readonly IExerciseRepository _exercises;

    public EnsureSamplePlanTemplatesCommandHandler(IPlanTemplateRepository templates, IExerciseRepository exercises)
    {
        _templates = templates;
        _exercises = exercises;
    }

    public async Task<Result<int>> Handle(EnsureSamplePlanTemplatesCommand request, CancellationToken cancellationToken)
    {
        if (await _templates.AnyForGymAsync(request.GymId, cancellationToken))
            return Result.Success(0);

        var library = await GlobalExerciseLookup.LoadAsync(_exercises, request.GymId, cancellationToken);
        var samples = SamplePlanTemplates.All
            .Select(s => PlanTemplate.CreateSample(request.GymId, s.Key, SamplePlanTemplates.ToContent(s, library)))
            .ToList();
        return Result.Success(await _templates.TryAddSamplesAsync(samples, cancellationToken));
    }
}
