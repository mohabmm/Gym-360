using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Scheduling.Application.Commands.ReverseSession;

/// <summary>Desk accepts a trainee's dispute: the session stops counting and the package session is given back.</summary>
public sealed record ReverseSessionCommand(Guid SessionId, Guid GymId) : ICommand;
