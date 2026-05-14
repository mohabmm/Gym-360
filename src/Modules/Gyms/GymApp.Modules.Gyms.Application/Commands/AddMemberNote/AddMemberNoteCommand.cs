using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.AddMemberNote;

public sealed record AddMemberNoteCommand(
    Guid GymId,
    Guid MemberId,
    Guid CreatedBy,
    string Content) : ICommand<Guid>;
