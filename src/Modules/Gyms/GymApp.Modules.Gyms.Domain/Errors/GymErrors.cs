using GymApp.Common.Domain;

namespace GymApp.Modules.Gyms.Domain.Errors;

public static class GymErrors
{
    public static readonly Error NotFound = new("Gyms.NotFound", "The gym was not found.");
    public static readonly Error GymCodeAlreadyExists = new("Gyms.GymCodeAlreadyExists", "A gym with this code already exists.");
    public static readonly Error AlreadySuspended = new("Gyms.AlreadySuspended", "The gym is already suspended.");
    public static readonly Error AlreadyActive = new("Gyms.AlreadyActive", "The gym is already active.");
    public static readonly Error NotApproved = new("Gyms.NotApproved", "The gym is not in an active state.");
    public static readonly Error NotPendingApproval = new("Gyms.NotPendingApproval", "The gym is not pending approval.");
    public static readonly Error MemberAlreadyAdded = new("Gyms.MemberAlreadyAdded", "The member is already added to this gym.");
    public static readonly Error MemberNotFound = new("Gyms.MemberNotFound", "The member was not found in this gym.");
    public static readonly Error TrainerAlreadyAdded = new("Gyms.TrainerAlreadyAdded", "The trainer is already added to this gym.");
    public static readonly Error TrainerNotFound = new("Gyms.TrainerNotFound", "The trainer was not found in this gym.");
    public static readonly Error ClassFull = new("Gyms.ClassFull", "The class is full and waitlist is not enabled.");
    public static readonly Error ClassNotFound = new("Gyms.ClassNotFound", "The class was not found or is not active.");
    public static readonly Error AnnouncementNotFound = new("Gyms.AnnouncementNotFound", "The announcement was not found.");
    public static readonly Error ClassAlreadyBooked = new("Gyms.ClassAlreadyBooked", "You are already booked into this class.");
    public static readonly Error BookingNotFound = new("Gyms.BookingNotFound", "The booking was not found.");
    public static readonly Error InvalidCheckIn = new("Gyms.InvalidCheckIn", "The check-in is invalid.");
    public static readonly Error GymTerminated = new("Gyms.GymTerminated", "The gym has been terminated.");
    public static readonly Error DuplicateCheckIn = new("Gyms.DuplicateCheckIn", "A check-in already exists for this member today.");
    public static readonly Error TrainerRateNotFound = new("Gyms.TrainerRateNotFound", "No rate has been configured for this trainer at this gym.");
    public static readonly Error InvalidRate = new("Gyms.InvalidRate", "The rate must be between 0 and 10,000 EGP.");

    public static readonly Error DataImportJobNotFound = new("Gyms.DataImportJobNotFound", "The data import job was not found.");
    public static readonly Error DataImportInvalidTransition = new("Gyms.DataImportInvalidTransition", "The import job cannot transition to the requested state.");
    public static readonly Error DataImportInvalidFile = new("Gyms.DataImportInvalidFile", "The uploaded file is empty or not a supported format.");
    public static readonly Error DataImportFileTooLarge = new("Gyms.DataImportFileTooLarge", "The uploaded file exceeds the maximum size of 10 MB.");

    public static readonly Error KioskPinCurrentRequired = new("Gyms.KioskPinCurrentRequired", "The current kiosk PIN is required to change or remove it.");
    public static readonly Error KioskPinCurrentIncorrect = new("Gyms.KioskPinCurrentIncorrect", "The current kiosk PIN is incorrect.");
    public static readonly Error KioskPinNotSet = new("Gyms.KioskPinNotSet", "No kiosk PIN is set for this gym.");

    public static readonly Error MemberAlreadyFrozen = new("Gyms.MemberAlreadyFrozen", "The member's membership is already frozen.");
    public static readonly Error MemberNotFrozen = new("Gyms.MemberNotFrozen", "The member's membership is not frozen.");
    public static readonly Error MemberFrozen = new("Gyms.MemberFrozen", "The member's membership is frozen and cannot check in.");
    public static readonly Error TrainerRequestNotFound = new("Gyms.TrainerRequestNotFound", "No trainer request was found for this trainer at this gym.");
    public static readonly Error TrainerAlreadyApproved = new("Gyms.TrainerAlreadyApproved", "The trainer has already been approved.");
    public static readonly Error TrainerAlreadyRejected = new("Gyms.TrainerAlreadyRejected", "The trainer request has already been rejected.");
}
