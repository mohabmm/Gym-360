namespace GymApp.Modules.Identity.Application.DTOs;

/// <summary>
/// Identity preview returned by staff-lookup so reception can confirm a returning
/// member's identity before re-linking them (the "Add existing member" flow). Only
/// populated for users whose role is Trainee.
/// </summary>
public sealed record TraineeLookupResponse(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    // Gym where this trainee currently holds an ACTIVE membership, if any. Used by the
    // re-link endpoint to reject an active-elsewhere trainee up front (One-Gym rule)
    // BEFORE mutating gym state, so no partial membership is left behind.
    Guid? ActiveGymId);
