using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Identity.Application.Commands.AdminUsers;

/// <summary>Platform admin suspends or reactivates a user. Status: "active" | "suspended".</summary>
public sealed record SetAdminUserStatusCommand(Guid UserId, Guid? RequestedBy, string Status) : ICommand;
