using GymApp.Common.Application.Messaging;
using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Commands.RecordCheckIn;

public sealed record RecordCheckInCommand(
    Guid GymId,
    string Method,
    string? QrToken,
    string? MemberNumber,
    Guid? MemberId,
    Guid? StaffId) : ICommand<CheckInResultDto>;
