namespace GymApp.Modules.Gyms.Application.DTOs;

public sealed record GymResponse(
    Guid Id,
    Guid OwnerId,
    string Name,
    string? Description,
    AddressDto Address,
    ContactInfoDto Contact,
    string GymCode,
    string Status,
    string PaymentStatus,
    List<string> Amenities,
    BrandingResponse? Branding,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int MemberCount,
    int TrainerCount,
    string? OwnerName = null);
