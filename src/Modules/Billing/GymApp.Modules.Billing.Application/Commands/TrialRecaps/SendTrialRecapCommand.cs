using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Commands.TrialRecaps;

/// <summary>pt.trial_recap: the trial's coach sends the trainee a short plan and a recommended package size.</summary>
public sealed record SendTrialRecapCommand(
    Guid GymId,
    Guid PackageId,
    Guid TrainerId,
    string Note,
    int RecommendedSessions) : ICommand;
