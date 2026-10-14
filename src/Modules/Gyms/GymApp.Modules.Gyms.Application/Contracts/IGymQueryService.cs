using GymApp.Modules.Gyms.Application.DTOs;

namespace GymApp.Modules.Gyms.Application.Contracts;

public interface IGymQueryService
{
    Task<PagedResponse<GymResponse>> ListGymsAsync(int page, int pageSize, string? nameFilter, CancellationToken cancellationToken = default);
    Task<List<GymResponse>> ListPendingGymsAsync(CancellationToken cancellationToken = default);
    Task<GymResponse?> GetGymByIdAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<bool> IsMemberOfGymAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
    Task<bool> IsTrainerAtGymAsync(Guid gymId, Guid trainerId, CancellationToken cancellationToken = default);
    Task<PagedResponse<MemberDto>> GetMembersAsync(Guid gymId, MemberRosterRequest request, CancellationToken cancellationToken = default);
    Task<MemberDto?> GetMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
    Task<List<TrainerDto>> GetTrainersAsync(Guid gymId, string? status = null, CancellationToken cancellationToken = default);
    Task<SessionPolicyDto> GetSessionPolicyAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<TrainerDto?> GetTrainerByIdAsync(Guid gymId, Guid trainerId, CancellationToken cancellationToken = default);
    Task<List<MemberDto>?> GetTrainerClientsAsync(Guid gymId, Guid trainerId, CancellationToken cancellationToken = default);
    Task<List<GymClassDto>> GetClassesAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<GymClassDto?> GetClassByIdAsync(Guid gymId, Guid classId, CancellationToken cancellationToken = default);
    Task<List<MemberDto>?> GetClassWaitlistAsync(Guid gymId, Guid classId, CancellationToken cancellationToken = default);
    Task<List<MemberDto>?> GetClassAttendanceAsync(Guid gymId, Guid classId, CancellationToken cancellationToken = default);
    Task<PagedResponse<CheckInDto>> GetCheckInHistoryAsync(Guid gymId, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResponse<AnnouncementDto>> GetAnnouncementsAsync(Guid gymId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<GymAnalyticsResponse?> GetAnalyticsAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<PeakHoursAnalyticsResponse?> GetPeakHoursAsync(Guid gymId, DateTime from, DateTime to, int timezoneOffsetMinutes, CancellationToken cancellationToken = default);
    Task<List<StaffDto>> GetStaffAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<BrandingResponse?> GetBrandingAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<int> GetMemberCountAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<int> GetTrainerCountAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<int> GetStaffCountAsync(Guid gymId, CancellationToken cancellationToken = default);
}
