using GymApp.Common.Application.Messaging;

namespace GymApp.Modules.Identity.Application.Commands.VerifyOtp;

public sealed record VerifyOtpCommand(Guid UserId, string Code, string Purpose) : ICommand;
