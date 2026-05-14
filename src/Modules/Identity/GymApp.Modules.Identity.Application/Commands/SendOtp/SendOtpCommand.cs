using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Identity.Application.Commands.SendOtp;

public sealed record SendOtpCommand(Guid UserId, string Purpose) : ICommand;
