using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Billing.Application.Commands.TrialFollowUps;

/// <summary>
/// pt.trial_followups: daily pass that raises the day-3 and day-12 reminders for trainees who
/// finished a trial and have neither bought a package nor tapped "I'm interested".
/// Runs inside one tenant scope; returns how many reminders were raised.
/// </summary>
public sealed record SendTrialFollowUpsCommand : ICommand<int>;
