using GymApp.Modules.Gamification.Shared;
using GymApp.Modules.Identity.Shared;
using GymApp.Modules.Billing.Shared;
using GymApp.Modules.Gyms.Application.Contracts;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GymApp.Modules.Gyms.Infrastructure.Services;

internal sealed class GymQueryService : IGymQueryService
{
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
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);

        var gyms = await query
            .OrderBy(g => g.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = gyms.Select(gym => new GymResponse(
            gym.Id,
            gym.OwnerId,
            gym.Name,
            gym.Description,
            new AddressDto(
                gym.Address.Street,
                gym.Address.City,
                gym.Address.State,
                gym.Address.Country,
                gym.Address.ZipCode,
                gym.Address.Latitude,
                gym.Address.Longitude),
            new ContactInfoDto(
                gym.ContactInfo.Email,
                gym.ContactInfo.Phone,
                gym.ContactInfo.Website),
            gym.GymCode.Value,
            gym.Status.ToString(),
            gym.PaymentStatus.ToString(),
            gym.Amenities.ToList(),
            gym.Branding is not null
                ? new BrandingResponse(
                    gym.Branding.LogoUrl,
                    gym.Branding.PrimaryColor,
                    gym.Branding.SecondaryColor,
                    gym.Branding.CoverPhotoUrl)
                : null,
            gym.CreatedAt,
            gym.UpdatedAt,
            gym.MemberIds.Count,
            gym.TrainerIds.Count)).ToList();

        return new PagedResponse<GymResponse>(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<List<GymResponse>> ListPendingGymsAsync(CancellationToken cancellationToken = default)
    {
        var gyms = await _dbContext.Gyms
            .AsNoTracking()
            .Where(g => g.Status == GymStatus.PendingApproval)
            .OrderBy(g => g.CreatedAt)
            .ToListAsync(cancellationToken);

        return gyms.Select(gym => new GymResponse(
            gym.Id,
            gym.OwnerId,
            gym.Name,
            gym.Description,
            new AddressDto(
                gym.Address.Street,
                gym.Address.City,
                gym.Address.State,
                gym.Address.Country,
                gym.Address.ZipCode,
                gym.Address.Latitude,
                gym.Address.Longitude),
            new ContactInfoDto(
                gym.ContactInfo.Email,
                gym.ContactInfo.Phone,
                gym.ContactInfo.Website),
            gym.GymCode.Value,
            gym.Status.ToString(),
            gym.PaymentStatus.ToString(),
            gym.Amenities.ToList(),
            gym.Branding is not null
                ? new BrandingResponse(
                    gym.Branding.LogoUrl,
                    gym.Branding.PrimaryColor,
                    gym.Branding.SecondaryColor,
                    gym.Branding.CoverPhotoUrl)
                : null,
            gym.CreatedAt,
            gym.UpdatedAt,
            gym.MemberIds.Count,
            gym.TrainerIds.Count)).ToList();
    }

    public async Task<GymResponse?> GetGymByIdAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var gym = await _dbContext.Gyms
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gymId, cancellationToken);

        if (gym is null) return null;

        return new GymResponse(
            gym.Id,
            gym.OwnerId,
            gym.Name,
            gym.Description,
            new AddressDto(
                gym.Address.Street,
                gym.Address.City,
                gym.Address.State,
                gym.Address.Country,
                gym.Address.ZipCode,
                gym.Address.Latitude,
                gym.Address.Longitude),
            new ContactInfoDto(
                gym.ContactInfo.Email,
                gym.ContactInfo.Phone,
                gym.ContactInfo.Website),
            gym.GymCode.Value,
            gym.Status.ToString(),
            gym.PaymentStatus.ToString(),
            gym.Amenities.ToList(),
            gym.Branding is not null
                ? new BrandingResponse(
                    gym.Branding.LogoUrl,
                    gym.Branding.PrimaryColor,
                    gym.Branding.SecondaryColor,
                    gym.Branding.CoverPhotoUrl)
                : null,
            gym.CreatedAt,
            gym.UpdatedAt,
            gym.MemberIds.Count,
            gym.TrainerIds.Count);
    }

    public async Task<bool> IsMemberOfGymAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default)
    {
        var gym = await _dbContext.Gyms
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gymId, cancellationToken);

        return gym?.MemberIds.Contains(memberId) ?? false;
    }

    public async Task<bool> IsTrainerAtGymAsync(Guid gymId, Guid trainerId, CancellationToken cancellationToken = default)
    {
        var gym = await _dbContext.Gyms
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gymId, cancellationToken);

        return gym?.TrainerIds.Contains(trainerId) ?? false;
    }

    public async Task<PagedResponse<MemberDto>> GetMembersAsync(
        Guid gymId, int page, int pageSize, string? status, string? section, string? search,
        CancellationToken cancellationToken = default)
    {
        var gym = await _dbContext.Gyms
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gymId, cancellationToken);

        if (gym is null)
            return new PagedResponse<MemberDto>(new List<MemberDto>(), page, pageSize, 0, 0);

        // Member ids in the gym's stored order. Filters below narrow this set BEFORE
        // paging, so search/status/section span the whole roster — not just one fetched
        // page (the old behaviour silently hid members past the first client-side slice).
        var orderedIds = gym.MemberIds.ToList();

        var statusKey = status?.Trim().ToLowerInvariant();
        var frozenFilter = statusKey == "frozen";
        // Only "active"/"inactive" map to an Identity profile status; "frozen" is a gym
        // directory flag (handled here).
        var profileStatusFilter = statusKey is "active" or "inactive" ? statusKey : null;

        // Any other status value ("expired", typos) has no backing data — return an empty
        // page rather than silently falling through to the full roster.
        if (statusKey is not null && statusKey is not ("active" or "inactive" or "frozen"))
            return new PagedResponse<MemberDto>(new List<MemberDto>(), page, pageSize, 0, 0);
        var hasSection = !string.IsNullOrWhiteSpace(section);
        var hasSearch = !string.IsNullOrWhiteSpace(search);
        // The directory is needed for section / frozen / member-number search, and also
        // to exclude frozen members from an "active"/"inactive" filter (frozen wins in the UI).
        var needsDirectory = hasSection || frozenFilter || hasSearch || profileStatusFilter is not null;

        // gym_members holds section / member number / frozen flag for the whole gym.
        // Load once when a filter needs it; reuse the same dict for page enrichment.
        var directory = new Dictionary<Guid, (string? MemberNumber, string? Section, DateTime JoinedAt, bool IsFrozen)>();
        if (needsDirectory)
        {
            var rows = await _dbContext.GymMembers
                .AsNoTracking()
                .Where(m => m.GymId == gymId)
                .Select(m => new { m.MemberId, m.MemberNumber, m.Section, m.JoinedAt, m.IsFrozen })
                .ToListAsync(cancellationToken);
            foreach (var r in rows)
                directory[r.MemberId] = (r.MemberNumber, r.Section, r.JoinedAt, r.IsFrozen);
        }

        IEnumerable<Guid> filtered = orderedIds;

        if (hasSection)
            filtered = filtered.Where(id =>
                directory.TryGetValue(id, out var d) &&
                string.Equals(d.Section, section, StringComparison.OrdinalIgnoreCase));

        if (frozenFilter)
            filtered = filtered.Where(id => directory.TryGetValue(id, out var d) && d.IsFrozen);
        else if (profileStatusFilter is not null)
            // A frozen member always displays as "frozen", never active/inactive.
            filtered = filtered.Where(id => !(directory.TryGetValue(id, out var d) && d.IsFrozen));

        if (hasSearch || profileStatusFilter is not null)
        {
            var candidate = filtered.ToList();
            var byProfile = await _userProfileLookup.FilterProfileIdsAsync(
                candidate, hasSearch ? search : null, profileStatusFilter, cancellationToken);
            var allowed = byProfile.ToHashSet();

            if (hasSearch)
            {
                // Member number lives in the Gyms directory, not the Identity profile —
                // union those matches in so "search by member number" works too. When a
                // profile-status filter is also set, re-check the number matches against
                // it so e.g. an inactive member never leaks into an "active" search.
                var term = search!.Trim();
                var numberMatches = candidate
                    .Where(id => !allowed.Contains(id) &&
                                 directory.TryGetValue(id, out var d) &&
                                 d.MemberNumber is not null &&
                                 d.MemberNumber.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (numberMatches.Count > 0)
                {
                    var numberAllowed = profileStatusFilter is null
                        ? numberMatches
                        : (await _userProfileLookup.FilterProfileIdsAsync(
                            numberMatches, null, profileStatusFilter, cancellationToken)).ToList();
                    foreach (var id in numberAllowed)
                        allowed.Add(id);
                }
            }

            filtered = candidate.Where(allowed.Contains);
        }

        var filteredIds = filtered.ToList();
        var totalCount = filteredIds.Count;
        var totalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);

        var pagedIds = filteredIds
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var profiles = await _userProfileLookup.GetProfilesAsync(pagedIds, cancellationToken);

        // Reuse the full directory when it was loaded for filtering; otherwise fetch only
        // the page's rows (the cheap path when no filter is active).
        if (!needsDirectory && pagedIds.Count > 0)
        {
            var rows = await _dbContext.GymMembers
                .AsNoTracking()
                .Where(m => m.GymId == gymId && pagedIds.Contains(m.MemberId))
                .Select(m => new { m.MemberId, m.MemberNumber, m.Section, m.JoinedAt, m.IsFrozen })
                .ToListAsync(cancellationToken);
            foreach (var r in rows)
                directory[r.MemberId] = (r.MemberNumber, r.Section, r.JoinedAt, r.IsFrozen);
        }

        var pagedMembers = pagedIds
            .Select(id =>
            {
                profiles.TryGetValue(id, out var profile);
                var hasDir = directory.TryGetValue(id, out var dir);
                return new MemberDto(
                    MemberId: id,
                    GymId: gymId,
                    Section: hasDir ? dir.Section : section,
                    MemberNumber: hasDir ? dir.MemberNumber : null,
                    JoinedAt: hasDir ? dir.JoinedAt : gym.CreatedAt,
                    FirstName: profile?.FirstName,
                    LastName: profile?.LastName,
                    Email: profile?.Email,
                    PhoneNumber: profile?.PhoneNumber,
                    ProfilePhotoUrl: profile?.ProfilePhotoUrl,
                    Role: profile?.Role,
                    Status: profile?.Status,
                    IsFrozen: hasDir && dir.IsFrozen,
                    NationalId: profile?.NationalId);
            })
            .ToList();

        return new PagedResponse<MemberDto>(pagedMembers, page, pageSize, totalCount, totalPages);
    }

    public async Task<MemberDto?> GetMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default)
    {
        var gym = await _dbContext.Gyms
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gymId, cancellationToken);

        if (gym is null || !gym.MemberIds.Contains(memberId))
            return null;

        var profile = await _userProfileLookup.GetProfileAsync(memberId, cancellationToken);
        var directory = await _dbContext.GymMembers
            .AsNoTracking()
            .Where(m => m.GymId == gymId && m.MemberId == memberId)
            .Select(m => new { m.MemberNumber, m.Section, m.JoinedAt, m.IsFrozen })
            .FirstOrDefaultAsync(cancellationToken);

        return new MemberDto(
            MemberId: memberId,
            GymId: gymId,
            Section: directory?.Section,
            MemberNumber: directory?.MemberNumber,
            JoinedAt: directory?.JoinedAt ?? gym.CreatedAt,
            FirstName: profile?.FirstName,
            LastName: profile?.LastName,
            Email: profile?.Email,
            PhoneNumber: profile?.PhoneNumber,
            ProfilePhotoUrl: profile?.ProfilePhotoUrl,
            Role: profile?.Role,
            Status: profile?.Status,
            IsFrozen: directory?.IsFrozen ?? false,
            NationalId: profile?.NationalId);
    }

    public async Task<List<TrainerDto>> GetTrainersAsync(Guid gymId, string? status = null, CancellationToken cancellationToken = default)
    {
        var gym = await _dbContext.Gyms
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gymId, cancellationToken);

        if (gym is null) return new List<TrainerDto>();

        var requests = await _dbContext.GymTrainerRequests
            .AsNoTracking()
            .Where(r => r.GymId == gymId)
            .Select(r => new { r.TrainerId, r.Status, r.Bio, Specializations = r.Specializations.ToList() })
            .ToListAsync(cancellationToken);

        var requestMap = requests.ToDictionary(r => r.TrainerId);

        var activeIds = gym.TrainerIds.ToList();
        var pendingIds = requests
            .Where(r => r.Status == TrainerRequestStatus.Pending)
            .Select(r => r.TrainerId)
            .Where(id => !activeIds.Contains(id))
            .ToList();

        var allIds = activeIds.Concat(pendingIds).Distinct().ToList();
        if (allIds.Count == 0) return new List<TrainerDto>();

        var profiles = await _userProfileLookup.GetProfilesAsync(allIds, cancellationToken);
        var statsMap = await _trainerStatsLookup.GetBatchAsync(allIds, gymId, cancellationToken);

        var clientCounts = new Dictionary<Guid, int>();
        foreach (var id in allIds)
        {
            var clients = await _trainerAssignmentLookup.GetClientsForTrainerAsync(id, gymId, cancellationToken);
            clientCounts[id] = clients.Count;
        }

        // An id in the gym aggregate (or a trainer request) that resolves to no
        // Identity profile is orphaned data — e.g. a stale/garbage id left in
        // trainer_ids. Drop it so the list never surfaces a null-named ghost row.
        var dtos = allIds
            .Where(id => profiles.TryGetValue(id, out var p) && p is not null)
            .Select(id =>
            {
                profiles.TryGetValue(id, out var profile);
                var hasRequest = requestMap.TryGetValue(id, out var req);
                var trainerStatus = hasRequest
                    ? req!.Status.ToString()
                    : TrainerRequestStatus.Approved.ToString();
                var stats = statsMap.TryGetValue(id, out var s) ? s : new TrainerStats(0m, 0m);
                return new TrainerDto(
                    TrainerId: id,
                    GymId: gymId,
                    FirstName: profile?.FirstName,
                    LastName: profile?.LastName,
                    Email: profile?.Email,
                    PhoneNumber: profile?.PhoneNumber,
                    ProfilePhotoUrl: profile?.ProfilePhotoUrl,
                    Status: trainerStatus,
                    Specializations: hasRequest ? req!.Specializations : Array.Empty<string>(),
                    Bio: hasRequest ? req!.Bio : null,
                    ActiveMemberCount: clientCounts.TryGetValue(id, out var count) ? count : 0,
                    Rating: stats.Rating,
                    MonthlyScore: stats.MonthlyScore);
            })
            .ToList();

        return string.IsNullOrWhiteSpace(status)
            ? dtos
            : dtos.Where(d => string.Equals(d.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
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
        var gym = await _dbContext.Gyms
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gymId, cancellationToken);

        if (gym is null) return null;

        var isActive = gym.TrainerIds.Contains(trainerId);

        var request = await _dbContext.GymTrainerRequests
            .AsNoTracking()
            .Where(r => r.GymId == gymId && r.TrainerId == trainerId)
            .Select(r => new { r.Status, r.Bio, Specializations = r.Specializations.ToList() })
            .FirstOrDefaultAsync(cancellationToken);

        if (!isActive && request is null) return null;

        var profile = await _userProfileLookup.GetProfileAsync(trainerId, cancellationToken);

        // No backing Identity profile => orphaned id (stale/garbage data); treat as
        // not found rather than returning a ghost trainer with null name fields.
        if (profile is null) return null;

        var status = request is not null
            ? request.Status.ToString()
            : TrainerRequestStatus.Approved.ToString();

        var clients = await _trainerAssignmentLookup.GetClientsForTrainerAsync(trainerId, gymId, cancellationToken);
        var stats = await _trainerStatsLookup.GetAsync(trainerId, gymId, cancellationToken);

        return new TrainerDto(
            TrainerId: trainerId,
            GymId: gymId,
            FirstName: profile?.FirstName,
            LastName: profile?.LastName,
            Email: profile?.Email,
            PhoneNumber: profile?.PhoneNumber,
            ProfilePhotoUrl: profile?.ProfilePhotoUrl,
            Status: status,
            Specializations: request?.Specializations ?? (IReadOnlyList<string>)Array.Empty<string>(),
            Bio: request?.Bio,
            ActiveMemberCount: clients.Count,
            Rating: stats.Rating,
            MonthlyScore: stats.MonthlyScore);
    }

    public async Task<List<MemberDto>?> GetTrainerClientsAsync(Guid gymId, Guid trainerId, CancellationToken cancellationToken = default)
    {
        var gym = await _dbContext.Gyms
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gymId, cancellationToken);

        if (gym is null) return null;

        var isActive = gym.TrainerIds.Contains(trainerId);
        var hasRequest = await _dbContext.GymTrainerRequests
            .AsNoTracking()
            .AnyAsync(r => r.GymId == gymId && r.TrainerId == trainerId, cancellationToken);

        if (!isActive && !hasRequest) return null;

        var memberIds = await _trainerAssignmentLookup.GetClientsForTrainerAsync(trainerId, gymId, cancellationToken);
        if (memberIds.Count == 0) return new List<MemberDto>();

        var profiles = await _userProfileLookup.GetProfilesAsync(memberIds, cancellationToken);
        var directory = await _dbContext.GymMembers
            .AsNoTracking()
            .Where(m => m.GymId == gymId && memberIds.Contains(m.MemberId))
            .Select(m => new { m.MemberId, m.MemberNumber, m.Section, m.JoinedAt, m.IsFrozen })
            .ToDictionaryAsync(m => m.MemberId, cancellationToken);

        return memberIds
            .Select(id =>
            {
                profiles.TryGetValue(id, out var profile);
                directory.TryGetValue(id, out var dir);
                return new MemberDto(
                    MemberId: id,
                    GymId: gymId,
                    Section: dir?.Section,
                    MemberNumber: dir?.MemberNumber,
                    JoinedAt: dir?.JoinedAt ?? DateTime.UtcNow,
                    FirstName: profile?.FirstName,
                    LastName: profile?.LastName,
                    Email: profile?.Email,
                    PhoneNumber: profile?.PhoneNumber,
                    ProfilePhotoUrl: profile?.ProfilePhotoUrl,
                    Role: profile?.Role,
                    Status: profile?.Status,
                    IsFrozen: dir?.IsFrozen ?? false,
                    NationalId: profile?.NationalId);
            })
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
        string? trainerName = null;
        if (profiles is not null && profiles.TryGetValue(c.TrainerId, out var trainer) && trainer is not null)
        {
            trainerName = $"{trainer.FirstName} {trainer.LastName}".Trim();
        }

        return new GymClassDto(
            c.Id,
            c.GymId,
            c.Name,
            c.Description,
            c.TrainerId,
            string.IsNullOrWhiteSpace(trainerName) ? null : trainerName,
            new ClassScheduleDto(
                c.Schedule.DayOfWeek,
                c.Schedule.StartTime,
                c.Schedule.DurationMinutes,
                c.Schedule.IsRecurring),
            c.Capacity,
            c.WaitlistEnabled,
            c.Status.ToString(),
            c.Bookings.Count(b => b.Status == BookingStatus.Confirmed),
            c.CreatedAt);
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
        var directory = await _dbContext.GymMembers
            .AsNoTracking()
            .Where(m => m.GymId == gymId && memberIds.Contains(m.MemberId))
            .Select(m => new { m.MemberId, m.MemberNumber, m.Section, m.JoinedAt })
            .ToDictionaryAsync(m => m.MemberId, cancellationToken);

        return memberIds
            .Select(id =>
            {
                profiles.TryGetValue(id, out var profile);
                directory.TryGetValue(id, out var dir);
                return new MemberDto(
                    MemberId: id,
                    GymId: gymId,
                    Section: dir?.Section,
                    MemberNumber: dir?.MemberNumber,
                    JoinedAt: dir?.JoinedAt ?? DateTime.UtcNow,
                    FirstName: profile?.FirstName,
                    LastName: profile?.LastName,
                    Email: profile?.Email,
                    PhoneNumber: profile?.PhoneNumber,
                    ProfilePhotoUrl: profile?.ProfilePhotoUrl,
                    Role: profile?.Role,
                    Status: profile?.Status,
                    NationalId: profile?.NationalId);
            })
            .ToList();
    }

    public async Task<List<GymClassDto>> GetClassesAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var classes = await _dbContext.GymClasses
            .AsNoTracking()
            .Include(c => c.Bookings)
            .Where(c => c.GymId == gymId && c.Status != GymClassStatus.Cancelled)
            .ToListAsync(cancellationToken);

        var trainerIds = classes.Select(c => c.TrainerId).Distinct().ToList();
        var profiles = trainerIds.Count == 0
            ? null
            : await _userProfileLookup.GetProfilesAsync(trainerIds, cancellationToken);

        return classes.Select(c =>
        {
            string? trainerName = null;
            if (profiles is not null && profiles.TryGetValue(c.TrainerId, out var trainer) && trainer is not null)
            {
                trainerName = $"{trainer.FirstName} {trainer.LastName}".Trim();
            }
            return new GymClassDto(
                c.Id,
                c.GymId,
                c.Name,
                c.Description,
                c.TrainerId,
                string.IsNullOrWhiteSpace(trainerName) ? null : trainerName,
                new ClassScheduleDto(
                    c.Schedule.DayOfWeek,
                    c.Schedule.StartTime,
                    c.Schedule.DurationMinutes,
                    c.Schedule.IsRecurring),
                c.Capacity,
                c.WaitlistEnabled,
                c.Status.ToString(),
                c.Bookings.Count(b => b.Status == BookingStatus.Confirmed),
                c.CreatedAt);
        }).ToList();
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

        var memberIds = checkIns.Select(c => c.MemberId).Distinct().ToList();
        var profiles = memberIds.Count == 0
            ? null
            : await _userProfileLookup.GetProfilesAsync(memberIds, cancellationToken);

        var items = checkIns.Select(c =>
        {
            string? memberName = null;
            string? photoUrl = null;
            if (profiles is not null && profiles.TryGetValue(c.MemberId, out var p) && p is not null)
            {
                memberName = $"{p.FirstName} {p.LastName}".Trim();
                photoUrl = p.ProfilePhotoUrl;
            }
            return new CheckInDto(
                c.Id,
                c.GymId,
                c.MemberId,
                c.Method.ToString(),
                c.CheckedInAt,
                c.StaffId,
                string.IsNullOrWhiteSpace(memberName) ? null : memberName,
                photoUrl,
                null);
        }).ToList();

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
        var gym = await _dbContext.Gyms
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gymId, cancellationToken);

        if (gym is null) return null;

        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = todayStart.AddDays(-(int)todayStart.DayOfWeek);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var checkInsToday = await _dbContext.CheckIns
            .AsNoTracking()
            .CountAsync(c => c.GymId == gymId && c.CheckedInAt >= todayStart, cancellationToken);

        var checkInsThisWeek = await _dbContext.CheckIns
            .AsNoTracking()
            .CountAsync(c => c.GymId == gymId && c.CheckedInAt >= weekStart, cancellationToken);

        var checkInsThisMonth = await _dbContext.CheckIns
            .AsNoTracking()
            .CountAsync(c => c.GymId == gymId && c.CheckedInAt >= monthStart, cancellationToken);

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

        var totalStaff = await _dbContext.StaffRoles
            .AsNoTracking()
            .CountAsync(s => s.GymId == gymId, cancellationToken);

        var sixMonthsAgo = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);
        var memberJoins = await _dbContext.GymMembers
            .AsNoTracking()
            .Where(m => m.GymId == gymId)
            .Select(m => m.JoinedAt)
            .ToListAsync(cancellationToken);

        var monthLabels = new[] { "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec" };
        var memberGrowth = new List<MemberGrowthPoint>();
        for (var i = 0; i < 6; i++)
        {
            var bucketStart = sixMonthsAgo.AddMonths(i);
            var bucketEnd = bucketStart.AddMonths(1);
            var newCount = memberJoins.Count(d => d >= bucketStart && d < bucketEnd);
            var activeCount = memberJoins.Count(d => d < bucketEnd);
            memberGrowth.Add(new MemberGrowthPoint(monthLabels[bucketStart.Month - 1], activeCount, newCount));
        }

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
            memberGrowth);
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
        var gym = await _dbContext.Gyms
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gymId, cancellationToken);

        if (gym?.Branding is null) return null;

        return new BrandingResponse(
            gym.Branding.LogoUrl,
            gym.Branding.PrimaryColor,
            gym.Branding.SecondaryColor,
            gym.Branding.CoverPhotoUrl);
    }

    public async Task<int> GetMemberCountAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var gym = await _dbContext.Gyms
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gymId, cancellationToken);

        return gym?.MemberIds.Count ?? 0;
    }

    public async Task<int> GetTrainerCountAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        var gym = await _dbContext.Gyms
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gymId, cancellationToken);

        return gym?.TrainerIds.Count ?? 0;
    }

    public async Task<int> GetStaffCountAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StaffRoles
            .AsNoTracking()
            .CountAsync(s => s.GymId == gymId, cancellationToken);
    }
}
