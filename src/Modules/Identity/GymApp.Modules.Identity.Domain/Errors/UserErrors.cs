using GymApp.Common.Domain;

namespace GymApp.Modules.Identity.Domain.Errors;

public static class UserErrors
{
    public static Error NotFound(Guid id) => new("User.NotFound", $"User with ID '{id}' was not found.");

    public static readonly Error EmailAlreadyExists = new("User.EmailAlreadyExists", "A user with this email already exists.");
    public static readonly Error InvalidCredentials = new("User.InvalidCredentials", "The provided credentials are invalid.");
    public static readonly Error EmailNotVerified = new("User.EmailNotVerified", "Email address has not been verified.");
    public static readonly Error AccountSuspended = new("User.AccountSuspended", "This account has been suspended.");
    public static readonly Error AccountDeleted = new("User.AccountDeleted", "This account has been deleted.");
    public static readonly Error InvalidOtp = new("User.InvalidOtp", "The provided OTP code is invalid.");
    public static readonly Error OtpExpired = new("User.OtpExpired", "The OTP code has expired.");
    public static readonly Error OtpRateLimitExceeded = new("User.OtpRateLimitExceeded", "Too many OTP requests. Please try again later.");
    public static readonly Error RefreshTokenExpired = new("User.RefreshTokenExpired", "The refresh token has expired.");
    public static readonly Error RefreshTokenRevoked = new("User.RefreshTokenRevoked", "The refresh token has been revoked.");
    public static readonly Error WeakPassword = new("User.WeakPassword", "The password does not meet minimum strength requirements.");
    public static readonly Error InvalidEmail = new("User.InvalidEmail", "The provided email address is not valid.");
    public static readonly Error InvalidPhoneNumber = new("User.InvalidPhoneNumber", "The provided phone number is not valid.");
    public static readonly Error UnderMinimumAge = new("User.UnderMinimumAge", "User does not meet the minimum age requirement.");
    public static readonly Error AlreadyLinkedToGym = new("User.AlreadyLinkedToGym", "User is already linked to this gym with the specified role.");
    public static readonly Error TraineeMembershipMissingMemberNumber = new(
        "User.TraineeMembershipMissingMemberNumber",
        "Trainee gym memberships must be created with a non-empty MemberNumber. Register the user via IGymMemberDirectory.RegisterAsync to obtain one.");
    public static readonly Error EmailReservedForOtherRole = new(
        "User.EmailReservedForOtherRole",
        "An account with this email already exists with another role (e.g. owner, admin, trainer). Use a different email/phone.");
    public static readonly Error PhoneReservedForOtherRole = new(
        "User.PhoneReservedForOtherRole",
        "An account with this phone already exists with another role (e.g. owner, admin, trainer). Use a different email/phone.");
}
