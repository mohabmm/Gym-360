using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Gyms.Application.Commands.UpdateSections;

public sealed record UpdateSectionsCommand(
    Guid GymId,
    List<string> Sections) : ICommand;
