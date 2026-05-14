using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Identity.Application.Commands.RecordConsent;

public sealed record RecordConsentCommand(
    Guid UserId,
    string ConsentType,
    bool IsGranted,
    string ConsentText,
    string IpAddress) : ICommand;
