using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.ValueObjects;

namespace GymApp.Modules.Gyms.Application.Contracts;

public interface IGymQueryService
{
    Task<PagedResponse<GymResponse>> ListGymsAsync(int page, int pageSize, string? nameFilter, CancellationToken cancellationToken = default);
    Task<List<GymResponse>> ListPendingGymsAsync(CancellationToken cancellationToken = default);
    Task<GymResponse?> GetGymByIdAsync(GymId gymId, CancellationToken cancellationToken = default);
    Task<bool> IsMemberOfGymAsync(GymId gymId, MemberId memberId, CancellationToken cancellationToken = default);
    Task<bool> IsTrainerAtGymAsync(GymId gymId, TrainerId trainerId, CancellationToken cancellationToken = default);
    Task<PagedResponse<MemberDto>> GetMembersAsync(GymId gymId, MemberRosterRequest request, CancellationToken cancellationToken = default);
    Task<MemberDto?> GetMemberAsync(GymId gymId, MemberId memberId, CancellationToken cancellationToken = default);
    Task<List<TrainerDto>> GetTrainersAsync(GymId gymId, string? status = null, CancellationToken cancellationToken = default);
    Task<SessionPolicyDto> GetSessionPolicyAsync(GymId gymId, CancellationToken cancellationToken = default);
    Task<TrainerDto?> GetTrainerByIdAsync(GymId gymId, TrainerId trainerId, CancellationToken cancellationToken = default);
    Task<List<MemberDto>?> GetTrainerClientsAsync(GymId gymId, TrainerId trainerId, CancellationToken cancellationToken = default);
    Task<List<GymClassDto>> GetClassesAsync(GymId gymId, CancellationToken cancellationToken = default);
    Task<GymClassDto?> GetClassByIdAsync(GymId gymId, Guid classId, CancellationToken cancellationToken = default);
    Task<List<MemberDto>?> GetClassWaitlistAsync(GymId gymId, Guid classId, CancellationToken cancellationToken = default);
    Task<List<MemberDto>?> GetClassAttendanceAsync(GymId gymId, Guid classId, CancellationToken cancellationToken = default);
    Task<PagedResponse<CheckInDto>> GetCheckInHistoryAsync(GymId gymId, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResponse<AnnouncementDto>> GetAnnouncementsAsync(GymId gymId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<GymAnalyticsResponse?> GetAnalyticsAsync(GymId gymId, CancellationToken cancellationToken = default);
    Task<PeakHoursAnalyticsResponse?> GetPeakHoursAsync(GymId gymId, DateTime from, DateTime to, int timezoneOffsetMinutes, CancellationToken cancellationToken = default);
    Task<List<StaffDto>> GetStaffAsync(GymId gymId, CancellationToken cancellationToken = default);
    Task<BrandingResponse?> GetBrandingAsync(GymId gymId, CancellationToken cancellationToken = default);
    Task<int> GetMemberCountAsync(GymId gymId, CancellationToken cancellationToken = default);
    Task<int> GetTrainerCountAsync(GymId gymId, CancellationToken cancellationToken = default);
    Task<int> GetStaffCountAsync(GymId gymId, CancellationToken cancellationToken = default);
}
