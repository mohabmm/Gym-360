import 'package:gymapp_core/gymapp_core.dart';

/// Staff permissions that can be checked against the user's role.
enum StaffPermission {
  // Dashboard
  viewDashboard,
  viewAnalytics,

  // Members
  viewMembers,
  addMember,
  editMember,
  freezeMember,
  deleteMember,

  // Trainers
  viewTrainers,
  approveTrainer,

  // Classes
  viewClasses,
  createClass,
  editClass,
  cancelClass,

  // Exercises
  viewExercises,
  createCustomExercise,

  // Check-in
  viewCheckInLog,
  manualCheckIn,
  kioskMode,

  // Billing
  recordPayment,
  viewPayments,
  managePackageTemplates,
  assignPackage,
  viewRevenue,

  // Announcements
  viewAnnouncements,
  createAnnouncement,

  // Gamification
  viewLeaderboards,
  viewTrainerRatings,
  moderateTestimonials,

  // Staff
  viewStaff,
  addStaff,
  editStaff,

  // Settings
  viewSettings,
  editGymProfile,
  editBranding,
  editSessionPolicies,
  editSections,
  editOperatingHours,
  editSeasonalHours,
  editAmenities,
  editNotificationSettings,
}

/// Central permission matrix for the gym staff app.
/// Owner: all access
/// Manager: all except billing settings (can still record payments)
/// Receptionist: check-in, record payments, view members only
class StaffPermissions {
  StaffPermissions._();

  /// Permissions granted to each role.
  static const Map<String, Set<StaffPermission>> _rolePermissions = {
    AppConstants.roleOwner: _ownerPermissions,
    AppConstants.roleManager: _managerPermissions,
    AppConstants.roleReceptionist: _receptionistPermissions,
  };

  static const Set<StaffPermission> _ownerPermissions = {
    ...StaffPermission.values,
  };

  static const Set<StaffPermission> _managerPermissions = {
    StaffPermission.viewDashboard,
    StaffPermission.viewAnalytics,
    StaffPermission.viewMembers,
    StaffPermission.addMember,
    StaffPermission.editMember,
    StaffPermission.freezeMember,
    StaffPermission.viewTrainers,
    StaffPermission.approveTrainer,
    StaffPermission.viewClasses,
    StaffPermission.createClass,
    StaffPermission.editClass,
    StaffPermission.cancelClass,
    StaffPermission.viewExercises,
    StaffPermission.createCustomExercise,
    StaffPermission.viewCheckInLog,
    StaffPermission.manualCheckIn,
    StaffPermission.kioskMode,
    StaffPermission.recordPayment,
    StaffPermission.viewPayments,
    StaffPermission.assignPackage,
    StaffPermission.viewRevenue,
    StaffPermission.viewAnnouncements,
    StaffPermission.createAnnouncement,
    StaffPermission.viewLeaderboards,
    StaffPermission.viewTrainerRatings,
    StaffPermission.moderateTestimonials,
    StaffPermission.viewStaff,
    StaffPermission.viewSettings,
    StaffPermission.editGymProfile,
    StaffPermission.editSessionPolicies,
    StaffPermission.editSections,
    StaffPermission.editOperatingHours,
    StaffPermission.editSeasonalHours,
    StaffPermission.editAmenities,
    StaffPermission.editNotificationSettings,
    // Manager CANNOT: managePackageTemplates, addStaff, editStaff,
    // editBranding, deleteMember
  };

  static const Set<StaffPermission> _receptionistPermissions = {
    StaffPermission.viewDashboard,
    StaffPermission.viewMembers,
    StaffPermission.viewTrainers,
    StaffPermission.viewCheckInLog,
    StaffPermission.manualCheckIn,
    StaffPermission.kioskMode,
    StaffPermission.recordPayment,
    StaffPermission.viewPayments,
    // Receptionist can attach a trainer to a member via the quick-assign
    // flow (member detail → "Assign Trainer"). Existing AssignSessionPackage
    // template flow stays manager+ since it touches pricing/templates.
    StaffPermission.assignPackage,
    StaffPermission.viewClasses,
    StaffPermission.viewAnnouncements,
  };

  /// Check if a role has a specific permission.
  static bool hasPermission(String role, StaffPermission permission) {
    final permissions = _rolePermissions[role.toLowerCase()];
    if (permissions == null) return false;
    return permissions.contains(permission);
  }

  /// Get all permissions for a role.
  static Set<StaffPermission> permissionsForRole(String role) {
    return _rolePermissions[role.toLowerCase()] ?? {};
  }

  /// Get the display name for a role in Arabic and English.
  static String roleDisplayName(String role, {bool isArabic = true}) {
    switch (role.toLowerCase()) {
      case AppConstants.roleOwner:
        return isArabic ? 'مالك' : 'Owner';
      case AppConstants.roleManager:
        return isArabic ? 'مدير' : 'Manager';
      case AppConstants.roleReceptionist:
        return isArabic ? 'موظف استقبال' : 'Receptionist';
      default:
        return role;
    }
  }
}
