using GymApp.Modules.Gamification.Shared;
using GymApp.Modules.Identity.Shared;
using GymApp.Modules.Billing.Shared;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gyms.Infrastructure.Services;

internal sealed class GymQueryService : IGymQueryService
{
    private const int MemberGrowthMonths = 6;

    private static readonly string[] MonthLabels =
        { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    private readonly GymsDbContext _dbContext;
    private readonly IUserProfileLookup _userProfileLookup;
    private readonly ITrainerAssignmentLookup _trainerAssignmentLookup;
    private readonly ITrainerStatsLookup _trainerStatsLookup;

    public GymQueryService(
        GymsDbContext dbContext,
        IUserProfileLookup userProfileLookup,
        ITrainerAssignmentLookup trainerAssignmentLookup,
        ITrainerStatsLookup trainerStatsLookup)
    {
        _dbContext = dbContext;
        _userProfileLookup = userProfileLookup;
        _trainerAssignmentLookup = trainerAssignmentLookup;
        _trainerStatsLookup = trainerStatsLookup;
    }

    public async Task<PagedResponse<GymResponse>> ListGymsAsync(
        int page, int pageSize, string? nameFilter, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _dbContext.Gyms.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(nameFilter))
        {
            var pattern = $"%{nameFilter.Trim()}%";
            query = query.Where(g => EF.Functions.Like(g.Name, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var gyms = await query
            .OrderBy(g => g.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = await ToGymResponsesAsync(gyms, cancellationToken);

        return new PagedResponse<GymResponse>(items, page, pageSize, totalCount, TotalPages(totalCount, pageSize));
    }

    public async Task<List<GymResponse>> ListPendingGymsAsync(CancellationToken cancellationToken = default)
    {
        var gyms = await _dbContext.Gyms
            .AsNoTracking()
            .Where(g => g.Status == GymStatus.PendingApproval)
            .OrderBy(g => g.CreatedAt)
            .ToListAsync(cancellationToken);

        return await ToGymResponsesAsync(gyms, cancellationToken);
    }

    public async Task<GymResponse?> GetGymByIdAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var gym = await FindGymAsync(gymId, cancellationToken);

        if (gym is null) return null;

        var ownerProfile = await _userProfileLookup.GetProfileAsync(gym.OwnerId, cancellationToken);

        return GymQueryMappings.ToGymResponse(gym, ownerProfile);
    }

    public async Task<bool> IsMemberOfGymAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default)
    {
        var gym = await FindGymAsync(gymId, cancellationToken);

        return gym?.MemberIds.Contains(memberId) ?? false;
    }

    public async Task<bool> IsTrainerAtGymAsync(Guid gymId, Guid trainerId, CancellationToken cancellationToken = default)
    {
        var gym = await FindGymAsync(gymId, cancellationToken);

        return gym?.TrainerIds.Contains(trainerId) ?? false;
    }

    public async Task<PagedResponse<MemberDto>> GetMembersAsync(
        Guid gymId, int page, int pageSize, string? status, string? section, string? search,
        CancellationToken cancellationToken = default,
        IReadOnlyCollection<Guid>? restrictToMemberIds = null)
    {
        var gym = await FindGymAsync(gymId, cancellationToken);

        // An unsupported status value has no backing data — return an empty page rather
        // than silently falling through to the full roster.
        var filter = MemberRosterFilter.Parse(status, section, search);
        if (gym is null || !filter.IsStatusSupported)
            return new PagedResponse<MemberDto>(new List<MemberDto>(), page, pageSize, 0, 0);

        // gym_members holds section / member number / frozen flag for the whole gym.
        // Load once when a filter needs it; reuse the same dict for page enrichment.
        var directory = filter.NeedsDirectory
            ? await LoadMemberDirectoryAsync(gymId, null, cancellationToken)
            : new Dictionary<Guid, MemberDirectoryEntry>();

        // Filters narrow the roster BEFORE paging, so search/status/section span the
        // whole roster — not just one fetched page (the old behaviour silently hid
        // members past the first client-side slice).
        var filteredIds = await FilterRosterAsync(
            RosterMemberIds(gym, restrictToMemberIds), filter, directory, cancellationToken);

        var pagedIds = filteredIds
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var profiles = await _userProfileLookup.GetProfilesAsync(pagedIds, cancellationToken);

        // Reuse the full directory when it was loaded for filtering; otherwise fetch only
        // the page's rows (the cheap path when no filter is active).
        if (!filter.NeedsDirectory && pagedIds.Count > 0)
            directory = await LoadMemberDirectoryAsync(gymId, pagedIds, cancellationToken);

        var pagedMembers = pagedIds
            .Select(id => GymQueryMappings.ToMemberDto(
                id, gymId, profiles.GetValueOrDefault(id), directory.GetValueOrDefault(id), gym.CreatedAt, section))
            .ToList();

        return new PagedResponse<MemberDto>(
            pagedMembers, page, pageSize, filteredIds.Count, TotalPages(filteredIds.Count, pageSize));
    }

    // Member ids in the gym's stored order.
    private static List<Guid> RosterMemberIds(Gym gym, IReadOnlyCollection<Guid>? restrictToMemberIds)
    {
        // Drop a ghost id equal to the gym's own id: stale/garbage data left in
        // member_ids resolves to no Identity profile and would surface as a null-named
        // row (and crash strict mobile parsers). Mirrors the trainer-list ghost guard.
        var memberIds = gym.MemberIds.Where(id => id != gym.Id);

        // Trainer-scoped roster: narrow to the caller-supplied ids up front so paging
        // and totals are computed over the trainer's clients, not the whole gym.
        if (restrictToMemberIds is not null)
        {
            var restrictTo = restrictToMemberIds.ToHashSet();
            memberIds = memberIds.Where(restrictTo.Contains);
        }

        return memberIds.ToList();
    }

    private async Task<List<Guid>> FilterRosterAsync(
        List<Guid> memberIds,
        MemberRosterFilter filter,
        IReadOnlyDictionary<Guid, MemberDirectoryEntry> directory,
        CancellationToken cancellationToken)
    {
        var candidates = filter.ApplyDirectoryFilters(memberIds, directory).ToList();
        if (!filter.NeedsProfileLookup)
            return candidates;

        var byProfile = await _userProfileLookup.FilterProfileIdsAsync(
            candidates, filter.HasSearch ? filter.Search : null, filter.ProfileStatus, cancellationToken);
        var allowed = byProfile.ToHashSet();

        // Union member-number matches in so "search by member number" works too.
        if (filter.HasSearch)
        {
            var numberMatches = filter.MatchMemberNumbers(
                candidates.Where(id => !allowed.Contains(id)), directory);
            allowed.UnionWith(await RestrictToProfileStatusAsync(numberMatches, filter.ProfileStatus, cancellationToken));
        }

        return candidates.Where(allowed.Contains).ToList();
    }

    // When a profile-status filter is set, re-check member-number matches against it
    // so e.g. an inactive member never leaks into an "active" search.
    private async Task<IReadOnlyCollection<Guid>> RestrictToProfileStatusAsync(
        List<Guid> memberIds, string? profileStatus, CancellationToken cancellationToken)
    {
        if (memberIds.Count == 0 || profileStatus is null)
            return memberIds;

        return await _userProfileLookup.FilterProfileIdsAsync(memberIds, null, profileStatus, cancellationToken);
    }

    public async Task<MemberDto?> GetMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default)
    {
        var gym = await FindGymAsync(gymId, cancellationToken);

        // memberId == gymId is a ghost id (stale data in member_ids), not a real member.
        if (gym is null || memberId == gymId || !gym.MemberIds.Contains(memberId))
            return null;

        var profile = await _userProfileLookup.GetProfileAsync(memberId, cancellationToken);
        var directory = await LoadMemberDirectoryAsync(gymId, new[] { memberId }, cancellationToken);

        return GymQueryMappings.ToMemberDto(
            memberId, gymId, profile, directory.GetValueOrDefault(memberId), gym.CreatedAt);
    }

    public async Task<List<TrainerDto>> GetTrainersAsync(Guid gymId, string? status = null, CancellationToken cancellationToken = default)
    {
        var gym = await FindGymAsync(gymId, cancellationToken);

        if (gym is null) return new List<TrainerDto>();

        var requests = await TrainerRequests(gymId).ToListAsync(cancellationToken);
        var requestMap = requests.ToDictionary(r => r.TrainerId);

        var activeIds = gym.TrainerIds.ToList();
        var pendingIds = requests
            .Where(r => r.Status == TrainerRequestStatus.Pending)
            .Select(r => r.TrainerId)
            .Where(id => !activeIds.Contains(id));

        var allIds = activeIds.Concat(pendingIds).Distinct().ToList();
        if (allIds.Count == 0) return new List<TrainerDto>();

        var profiles = await _userProfileLookup.GetProfilesAsync(allIds, cancellationToken);
        var statsMap = await _trainerStatsLookup.GetBatchAsync(allIds, gymId, cancellationToken);
        var clientCounts = await CountClientsAsync(allIds, gymId, cancellationToken);

        // An id in the gym aggregate (or a trainer request) that resolves to no
        // Identity profile is orphaned data — e.g. a stale/garbage id left in
        // trainer_ids. Drop it so the list never surfaces a null-named ghost row.
        var dtos = allIds
            .Where(id => profiles.GetValueOrDefault(id) is not null)
            .Select(id => GymQueryMappings.ToTrainerDto(
                id,
                gymId,
                profiles.GetValueOrDefault(id),
                requestMap.GetValueOrDefault(id),
                clientCounts.GetValueOrDefault(id),
                statsMap.GetValueOrDefault(id) ?? new TrainerStats(0m, 0m)))
            .ToList();

        return string.IsNullOrWhiteSpace(status)
            ? dtos
            : dtos.Where(d => string.Equals(d.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private async Task<Dictionary<Guid, int>> CountClientsAsync(
        IEnumerable<Guid> trainerIds, Guid gymId, CancellationToken cancellationToken)
    {
        var clientCounts = new Dictionary<Guid, int>();
        foreach (var id in trainerIds)
        {
            var clients = await _trainerAssignmentLookup.GetClientsForTrainerAsync(id, gymId, cancellationToken);
            clientCounts[id] = clients.Count;
        }

        return clientCounts;
    }

    public async Task<SessionPolicyDto> GetSessionPolicyAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var policy = await _dbContext.GymSessionPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.GymId == gymId, cancellationToken);

        // Return defaults matching GymSessionPolicy.Create when no policy exists yet.
        if (policy is null)
            return new SessionPolicyDto(gymId, 24, 3, 30, "FlagOnly");

        return new SessionPolicyDto(
            policy.GymId,
            policy.CancellationWindowHours,
            policy.NoshowThreshold,
            policy.NoshowWindowDays,
            policy.NoshowAction.ToString());
    }

    public async Task<TrainerDto?> GetTrainerByIdAsync(Guid gymId, Guid trainerId, CancellationToken cancellationToken = default)
    {
        var gym = await FindGymAsync(gymId, cancellationToken);

        if (gym is null) return null;

        var request = await TrainerRequests(gymId, trainerId).FirstOrDefaultAsync(cancellationToken);

        if (!gym.TrainerIds.Contains(trainerId) && request is null) return null;

        var profile = await _userProfileLookup.GetProfileAsync(trainerId, cancellationToken);

        // No backing Identity profile => orphaned id (stale/garbage data); treat as
        // not found rather than returning a ghost trainer with null name fields.
        if (profile is null) return null;

        var clients = await _trainerAssignmentLookup.GetClientsForTrainerAsync(trainerId, gymId, cancellationToken);
        var stats = await _trainerStatsLookup.GetAsync(trainerId, gymId, cancellationToken);

        return GymQueryMappings.ToTrainerDto(trainerId, gymId, profile, request, clients.Count, stats);
    }

    public async Task<List<MemberDto>?> GetTrainerClientsAsync(Guid gymId, Guid trainerId, CancellationToken cancellationToken = default)
    {
        var gym = await FindGymAsync(gymId, cancellationToken);

        if (gym is null) return null;

        var isActive = gym.TrainerIds.Contains(trainerId);
        var hasRequest = await _dbContext.GymTrainerRequests
            .AsNoTracking()
            .AnyAsync(r => r.GymId == gymId && r.TrainerId == trainerId, cancellationToken);

        if (!isActive && !hasRequest) return null;

        var memberIds = await _trainerAssignmentLookup.GetClientsForTrainerAsync(trainerId, gymId, cancellationToken);
        if (memberIds.Count == 0) return new List<MemberDto>();

        var profiles = await _userProfileLookup.GetProfilesAsync(memberIds, cancellationToken);
        var directory = await LoadMemberDirectoryAsync(gymId, memberIds, cancellationToken);

        return memberIds
            .Select(id => GymQueryMappings.ToMemberDto(
                id, gymId, profiles.GetValueOrDefault(id), directory.GetValueOrDefault(id), DateTime.UtcNow))
            .ToList();
    }

    public async Task<GymClassDto?> GetClassByIdAsync(Guid gymId, Guid classId, CancellationToken cancellationToken = default)
    {
        var c = await _dbContext.GymClasses
            .AsNoTracking()
            .Include(c => c.Bookings)
            .FirstOrDefaultAsync(c => c.Id == classId && c.GymId == gymId && c.Status != GymClassStatus.Cancelled, cancellationToken);

        if (c is null)
            return null;

        var profiles = await _userProfileLookup.GetProfilesAsync(new[] { c.TrainerId }, cancellationToken);

        return GymQueryMappings.ToGymClassDto(c, profiles?.GetValueOrDefault(c.TrainerId));
    }

    public Task<List<MemberDto>?> GetClassWaitlistAsync(Guid gymId, Guid classId, CancellationToken cancellationToken = default)
        => GetClassBookingMembersAsync(gymId, classId, BookingStatus.Waitlisted, cancellationToken);

    public Task<List<MemberDto>?> GetClassAttendanceAsync(Guid gymId, Guid classId, CancellationToken cancellationToken = default)
        => GetClassBookingMembersAsync(gymId, classId, BookingStatus.Attended, cancellationToken);

    private async Task<List<MemberDto>?> GetClassBookingMembersAsync(
        Guid gymId, Guid classId, BookingStatus status, CancellationToken cancellationToken)
    {
        var classExists = await _dbContext.GymClasses
            .AsNoTracking()
            .AnyAsync(c => c.Id == classId && c.GymId == gymId && c.Status != GymClassStatus.Cancelled, cancellationToken);

        if (!classExists)
            return null;

        var memberIds = await _dbContext.GymClasses
            .AsNoTracking()
            .Where(c => c.Id == classId && c.GymId == gymId)
            .SelectMany(c => c.Bookings)
            .Where(b => b.Status == status)
            .OrderBy(b => b.BookedAt)
            .Select(b => b.MemberId)
            .ToListAsync(cancellationToken);

        if (memberIds.Count == 0)
            return new List<MemberDto>();

        var profiles = await _userProfileLookup.GetProfilesAsync(memberIds, cancellationToken);
        var directory = await LoadMemberDirectoryAsync(gymId, memberIds, cancellationToken);

        // Class rosters do not report the frozen flag.
        return memberIds
            .Select(id => GymQueryMappings.ToMemberDto(
                id, gymId, profiles.GetValueOrDefault(id), directory.GetValueOrDefault(id), DateTime.UtcNow)
                with { IsFrozen = false })
            .ToList();
    }

    public async Task<List<GymClassDto>> GetClassesAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var classes = await _dbContext.GymClasses
            .AsNoTracking()
            .Include(c => c.Bookings)
            .Where(c => c.GymId == gymId && c.Status != GymClassStatus.Cancelled)
            .ToListAsync(cancellationToken);

        var profiles = await GetProfilesOrEmptyAsync(
            classes.Select(c => c.TrainerId).Distinct().ToList(), cancellationToken);

        return classes
            .Select(c => GymQueryMappings.ToGymClassDto(c, profiles.GetValueOrDefault(c.TrainerId)))
            .ToList();
    }

    public async Task<PagedResponse<CheckInDto>> GetCheckInHistoryAsync(
        Guid gymId, DateTime? from, DateTime? to, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CheckIns
            .AsNoTracking()
            .Where(c => c.GymId == gymId);

        if (from.HasValue)
            query = query.Where(c => c.CheckedInAt >= from.Value);

        if (to.HasValue)
            query = query.Where(c => c.CheckedInAt <= to.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var checkIns = await query
            .OrderByDescending(c => c.CheckedInAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var profiles = await GetProfilesOrEmptyAsync(
            checkIns.Select(c => c.MemberId).Distinct().ToList(), cancellationToken);

        var items = checkIns
            .Select(c => GymQueryMappings.ToCheckInDto(c, profiles.GetValueOrDefault(c.MemberId)))
            .ToList();

        return new PagedResponse<CheckInDto>(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<PagedResponse<AnnouncementDto>> GetAnnouncementsAsync(
        Guid gymId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Announcements
            .AsNoTracking()
            .Where(a => a.GymId == gymId);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var announcements = await query
            .OrderByDescending(a => a.PublishedAt ?? a.ScheduledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = announcements.Select(a => new AnnouncementDto(
            a.Id,
            a.GymId,
            a.Title,
            a.Content,
            a.ImageUrl,
            a.Category.ToString(),
            a.TargetAudience.ToString(),
            a.PublishedAt,
            a.ScheduledAt,
            a.CreatedBy)).ToList();

        return new PagedResponse<AnnouncementDto>(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<GymAnalyticsResponse?> GetAnalyticsAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var gym = await FindGymAsync(gymId, cancellationToken);

        if (gym is null) return null;

        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = todayStart.AddDays(-(int)todayStart.DayOfWeek);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var checkInsToday = await CountCheckInsSinceAsync(gymId, todayStart, cancellationToken);
        var checkInsThisWeek = await CountCheckInsSinceAsync(gymId, weekStart, cancellationToken);
        var checkInsThisMonth = await CountCheckInsSinceAsync(gymId, monthStart, cancellationToken);

        var totalClasses = await _dbContext.GymClasses
            .AsNoTracking()
            .CountAsync(c => c.GymId == gymId, cancellationToken);

        var activeBookings = await _dbContext.ClassBookings
            .AsNoTracking()
            .Join(
                _dbContext.GymClasses.AsNoTracking().Where(c => c.GymId == gymId),
                b => b.GymClassId,
                c => c.Id,
                (b, c) => b)
            .CountAsync(b => b.Status == BookingStatus.Confirmed, cancellationToken);

        var totalStaff = await GetStaffCountAsync(gymId, cancellationToken);

        var memberJoins = await _dbContext.GymMembers
            .AsNoTracking()
            .Where(m => m.GymId == gymId)
            .Select(m => m.JoinedAt)
            .ToListAsync(cancellationToken);

        return new GymAnalyticsResponse(
            gymId,
            gym.MemberIds.Count,
            gym.TrainerIds.Count,
            totalStaff,
            totalClasses,
            checkInsToday,
            checkInsThisWeek,
            checkInsThisMonth,
            activeBookings,
            BuildMemberGrowth(memberJoins, monthStart.AddMonths(-(MemberGrowthMonths - 1))));
    }

    private Task<int> CountCheckInsSinceAsync(Guid gymId, DateTime since, CancellationToken cancellationToken)
        => _dbContext.CheckIns
            .AsNoTracking()
            .CountAsync(c => c.GymId == gymId && c.CheckedInAt >= since, cancellationToken);

    // One point per calendar month starting at firstMonthStart: members who joined in
    // that month, and the running total of members joined up to its end.
    private static List<MemberGrowthPoint> BuildMemberGrowth(List<DateTime> memberJoins, DateTime firstMonthStart)
    {
        var memberGrowth = new List<MemberGrowthPoint>();
        for (var i = 0; i < MemberGrowthMonths; i++)
        {
            var bucketStart = firstMonthStart.AddMonths(i);
            var bucketEnd = bucketStart.AddMonths(1);
            var newCount = memberJoins.Count(d => d >= bucketStart && d < bucketEnd);
            var activeCount = memberJoins.Count(d => d < bucketEnd);
            memberGrowth.Add(new MemberGrowthPoint(MonthLabels[bucketStart.Month - 1], activeCount, newCount));
        }

        return memberGrowth;
    }

    public async Task<PeakHoursAnalyticsResponse?> GetPeakHoursAsync(
        Guid gymId,
        DateTime from,
        DateTime to,
        int timezoneOffsetMinutes,
        CancellationToken cancellationToken = default)
    {
        var gymExists = await _dbContext.Gyms
            .AsNoTracking()
            .AnyAsync(g => g.Id == gymId, cancellationToken);
        if (!gymExists) return null;

        var timestamps = await _dbContext.CheckIns
            .AsNoTracking()
            .Where(c => c.GymId == gymId
                        && c.CheckedInAt >= from
                        && c.CheckedInAt < to)
            .Select(c => c.CheckedInAt)
            .ToListAsync(cancellationToken);

        // Bucket by local (gym-timezone) day-of-week + hour. We convert in
        // memory so we don't need AT TIME ZONE SQL or a gym.Timezone column;
        // for a 30-day window this is a few thousand rows at most.
        var offset = TimeSpan.FromMinutes(timezoneOffsetMinutes);
        var buckets = timestamps
            .Select(t => DateTime.SpecifyKind(t, DateTimeKind.Utc).Add(offset))
            .GroupBy(t => new { DayOfWeek = (int)t.DayOfWeek, Hour = t.Hour })
            .Select(g => new PeakHourBucket(g.Key.DayOfWeek, g.Key.Hour, g.Count()))
            .OrderBy(b => b.DayOfWeek)
            .ThenBy(b => b.Hour)
            .ToList();

        return new PeakHoursAnalyticsResponse(
            gymId, from, to, timezoneOffsetMinutes, timestamps.Count, buckets);
    }

    public async Task<List<StaffDto>> GetStaffAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var staffRoles = await _dbContext.StaffRoles
            .AsNoTracking()
            .Where(s => s.GymId == gymId)
            .ToListAsync(cancellationToken);

        if (staffRoles.Count == 0) return new List<StaffDto>();

        var userIds = staffRoles.Select(s => s.UserId).Distinct().ToList();
        var profiles = await _userProfileLookup.GetProfilesAsync(userIds, cancellationToken);

        return staffRoles.Select(s =>
        {
            profiles.TryGetValue(s.UserId, out var profile);
            return new StaffDto(
                Id: s.Id,
                GymId: s.GymId,
                UserId: s.UserId,
                Role: s.Role.ToString(),
                FirstName: profile?.FirstName,
                LastName: profile?.LastName,
                Email: profile?.Email,
                PhoneNumber: profile?.PhoneNumber,
                ProfilePhotoUrl: profile?.ProfilePhotoUrl);
        }).ToList();
    }

    public async Task<BrandingResponse?> GetBrandingAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var gym = await FindGymAsync(gymId, cancellationToken);

        return gym?.Branding is null ? null : GymQueryMappings.ToBrandingResponse(gym.Branding);
    }

    public async Task<int> GetMemberCountAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var gym = await FindGymAsync(gymId, cancellationToken);

        return gym?.MemberIds.Count ?? 0;
    }

    public async Task<int> GetTrainerCountAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var gym = await FindGymAsync(gymId, cancellationToken);

        return gym?.TrainerIds.Count ?? 0;
    }

    public async Task<int> GetStaffCountAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StaffRoles
            .AsNoTracking()
            .CountAsync(s => s.GymId == gymId, cancellationToken);
    }

    private Task<Gym?> FindGymAsync(Guid gymId, CancellationToken cancellationToken)
        => _dbContext.Gyms
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gymId, cancellationToken);

    private async Task<List<GymResponse>> ToGymResponsesAsync(List<Gym> gyms, CancellationToken cancellationToken)
    {
        var ownerProfiles = await _userProfileLookup.GetProfilesAsync(
            gyms.Select(g => g.OwnerId).Distinct(), cancellationToken);

        return gyms
            .Select(gym => GymQueryMappings.ToGymResponse(gym, ownerProfiles.GetValueOrDefault(gym.OwnerId)))
            .ToList();
    }

    private async Task<IReadOnlyDictionary<Guid, UserProfileInfo>> GetProfilesOrEmptyAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
        => userIds.Count == 0
            ? new Dictionary<Guid, UserProfileInfo>()
            : await _userProfileLookup.GetProfilesAsync(userIds, cancellationToken);

    // gym_members rows for the gym, keyed by member id; all members when memberIds is null.
    private async Task<Dictionary<Guid, MemberDirectoryEntry>> LoadMemberDirectoryAsync(
        Guid gymId, IReadOnlyCollection<Guid>? memberIds, CancellationToken cancellationToken)
    {
        var members = _dbContext.GymMembers
            .AsNoTracking()
            .Where(m => m.GymId == gymId);

        if (memberIds is not null)
            members = members.Where(m => memberIds.Contains(m.MemberId));

        var rows = await members
            .Select(m => new MemberDirectoryEntry(m.MemberId, m.MemberNumber, m.Section, m.JoinedAt, m.IsFrozen))
            .ToListAsync(cancellationToken);

        var directory = new Dictionary<Guid, MemberDirectoryEntry>();
        foreach (var row in rows)
            directory[row.MemberId] = row;

        return directory;
    }

    private IQueryable<TrainerRequestInfo> TrainerRequests(Guid gymId, Guid? trainerId = null)
    {
        var requests = _dbContext.GymTrainerRequests
            .AsNoTracking()
            .Where(r => r.GymId == gymId);

        if (trainerId.HasValue)
            requests = requests.Where(r => r.TrainerId == trainerId.Value);

        return requests.Select(r => new TrainerRequestInfo(r.TrainerId, r.Status, r.Bio, r.Specializations.ToList()));
    }

    private static int TotalPages(int totalCount, int pageSize)
        => pageSize <= 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);
}
