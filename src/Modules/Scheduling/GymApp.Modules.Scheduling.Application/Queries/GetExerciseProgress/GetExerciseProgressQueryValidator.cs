using FluentValidation;

namespace GymApp.Modules.Scheduling.Application.Queries.GetExerciseProgress;

internal sealed class GetExerciseProgressQueryValidator : AbstractValidator<GetExerciseProgressQuery>
{
    public GetExerciseProgressQueryValidator()
    {
        RuleFor(x => x.CallerId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.GymId).NotEmpty();
    }
}
