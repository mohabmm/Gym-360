using GymApp.Modules.Gamification.Shared;
using GymApp.Modules.Gyms.Application.DTOs;
using GymApp.Modules.Gyms.Domain.Entities;
using GymApp.Modules.Gyms.Domain.Enums;
using GymApp.Modules.Gyms.Domain.ValueObjects;
using GymApp.Modules.Identity.Shared;

namespace GymApp.Modules.Gyms.Infrastructure.Services;

// gym_members row projected for roster reads: section / member number / frozen flag.
internal sealed record MemberDirectoryEntry(
    Guid MemberId, string? MemberNumber, string? Section, DateTime JoinedAt, bool IsFrozen);

// gym_trainer_requests row projected for trainer reads.
internal sealed record TrainerRequestInfo(
    Guid TrainerId, TrainerRequestStatus Status, string? Bio, List<string> Specializations);

/// <summary>
/// Entity/profile → DTO projections shared by the <see cref="GymQueryService"/> read paths.
/// </summary>
internal static class GymQueryMappings
{
    public static GymResponse ToGymResponse(Gym gym, UserProfileInfo? owner) => new(
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
        gym.Branding is not null ? ToBrandingResponse(gym.Branding) : null,
        gym.CreatedAt,
        gym.UpdatedAt,
        gym.MemberIds.Count,
        gym.TrainerIds.Count,
        FormatOwnerName(owner));

    public static BrandingResponse ToBrandingResponse(Branding branding) => new(
        branding.LogoUrl,
        branding.PrimaryColor,
        branding.SecondaryColor,
        branding.CoverPhotoUrl);

    // Owner display name for gym listings/detail: "First Last", falling back to
    // email when the name is blank, or null when the owner profile is unknown.
    public static string? FormatOwnerName(UserProfileInfo? profile)
        => profile is null ? null : FullNameOrNull(profile) ?? profile.Email;

    // "First Last", or null when the profile is unknown or the name is blank.
    public static string? FullNameOrNull(UserProfileInfo? profile)
    {
        if (profile is null) return null;
        var name = $"{profile.FirstName} {profile.LastName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    // A member with no gym_members row falls back to the caller-supplied
    // join date / section; a member with no Identity profile keeps null name fields.
    public static MemberDto ToMemberDto(
        Guid memberId,
        Guid gymId,
        UserProfileInfo? profile,
        MemberDirectoryEntry? directory,
        DateTime joinedAtFallback,
        string? sectionFallback = null) => new(
        MemberId: memberId,
        GymId: gymId,
        Section: directory is not null ? directory.Section : sectionFallback,
        MemberNumber: directory?.MemberNumber,
        JoinedAt: directory?.JoinedAt ?? joinedAtFallback,
        FirstName: profile?.FirstName,
        LastName: profile?.LastName,
        Email: profile?.Email,
        PhoneNumber: profile?.PhoneNumber,
        ProfilePhotoUrl: profile?.ProfilePhotoUrl,
        Role: profile?.Role,
        Status: profile?.Status,
        IsFrozen: directory?.IsFrozen ?? false,
        NationalId: profile?.NationalId);

    // A trainer on the gym aggregate with no request row was added directly => Approved.
    public static TrainerDto ToTrainerDto(
        Guid trainerId,
        Guid gymId,
        UserProfileInfo? profile,
        TrainerRequestInfo? request,
        int activeMemberCount,
        TrainerStats stats) => new(
        TrainerId: trainerId,
        GymId: gymId,
        FirstName: profile?.FirstName,
        LastName: profile?.LastName,
        Email: profile?.Email,
        PhoneNumber: profile?.PhoneNumber,
        ProfilePhotoUrl: profile?.ProfilePhotoUrl,
        Status: (request?.Status ?? TrainerRequestStatus.Approved).ToString(),
        Specializations: request?.Specializations ?? (IReadOnlyList<string>)Array.Empty<string>(),
        Bio: request?.Bio,
        ActiveMemberCount: activeMemberCount,
        Rating: stats.Rating,
        MonthlyScore: stats.MonthlyScore);

    public static GymClassDto ToGymClassDto(GymClass gymClass, UserProfileInfo? trainer) => new(
        gymClass.Id,
        gymClass.GymId,
        gymClass.Name,
        gymClass.Description,
        gymClass.TrainerId,
        FullNameOrNull(trainer),
        new ClassScheduleDto(
            gymClass.Schedule.DayOfWeek,
            gymClass.Schedule.StartTime,
            gymClass.Schedule.DurationMinutes,
            gymClass.Schedule.IsRecurring),
        gymClass.Capacity,
        gymClass.WaitlistEnabled,
        gymClass.Status.ToString(),
        gymClass.Bookings.Count(b => b.Status == BookingStatus.Confirmed),
        gymClass.CreatedAt);

    public static CheckInDto ToCheckInDto(CheckIn checkIn, UserProfileInfo? member) => new(
        checkIn.Id,
        checkIn.GymId,
        checkIn.MemberId,
        checkIn.Method.ToString(),
        checkIn.CheckedInAt,
        checkIn.StaffId,
        FullNameOrNull(member),
        member?.ProfilePhotoUrl,
        null);
}
