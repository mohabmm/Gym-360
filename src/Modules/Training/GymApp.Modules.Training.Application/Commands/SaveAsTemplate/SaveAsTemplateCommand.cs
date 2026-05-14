using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Training.Application.Commands.SaveAsTemplate;

public sealed record SaveAsTemplateCommand(Guid WorkoutId) : ICommand;
