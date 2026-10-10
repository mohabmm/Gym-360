import 'dart:convert';

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

/// Central permission matrix for the gym staff app. Mirrors the default
/// matrix in docs/01_GYMAPP_BACKEND_COMPLETE.md ("Staff Roles & Permissions")
/// and PRODUCT_SPEC.md §1.4.
/// Owner: all access
/// Manager: everything except removing members (and subscription/billing
/// settings, which this app does not expose)
/// Receptionist: check-in, payments, view/add/edit/freeze members
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
    StaffPermission.managePackageTemplates,
    StaffPermission.assignPackage,
    StaffPermission.viewRevenue,
    StaffPermission.viewAnnouncements,
    StaffPermission.createAnnouncement,
    StaffPermission.viewLeaderboards,
    StaffPermission.viewTrainerRatings,
    StaffPermission.moderateTestimonials,
    StaffPermission.viewStaff,
    StaffPermission.addStaff,
    StaffPermission.editStaff,
    StaffPermission.viewSettings,
    StaffPermission.editGymProfile,
    StaffPermission.editBranding,
    StaffPermission.editSessionPolicies,
    StaffPermission.editSections,
    StaffPermission.editOperatingHours,
    StaffPermission.editSeasonalHours,
    StaffPermission.editAmenities,
    StaffPermission.editNotificationSettings,
    // Manager CANNOT: deleteMember
  };

  static const Set<StaffPermission> _receptionistPermissions = {
    StaffPermission.viewDashboard,
    StaffPermission.viewMembers,
    StaffPermission.addMember,
    StaffPermission.editMember,
    StaffPermission.freezeMember,
    StaffPermission.viewTrainers,
    StaffPermission.viewCheckInLog,
    StaffPermission.manualCheckIn,
    StaffPermission.kioskMode,
    StaffPermission.recordPayment,
    StaffPermission.viewPayments,
    // Receptionist assigns a template to a member at the desk when they pay
    // (story 75). Creating/pricing templates stays manager+.
    StaffPermission.assignPackage,
    StaffPermission.viewClasses,
    StaffPermission.viewExercises,
    StaffPermission.viewAnnouncements,
    StaffPermission.editNotificationSettings,
  };

  /// Resolves the app-side staff role from a freshly issued access token.
  ///
  /// The backend puts the per-gym tier in the `staffSubRole` claim
  /// (Owner/Manager/Receptionist) and marks owner principals with
  /// `staffRole=owner`. [identityRole] is the coarse `user.role` from the login
  /// response (`owner` / `staff`). Staff whose tier cannot be read get the
  /// least-privileged role rather than manager access.
  static String roleFromAccessToken(String accessToken, String identityRole) {
    final claims = _decodeJwtPayload(accessToken);
    final subRole = (claims['staffSubRole'] as String?)?.toLowerCase();
    if (subRole != null && _rolePermissions.containsKey(subRole)) {
      return subRole;
    }
    final staffRole = (claims['staffRole'] as String?)?.toLowerCase();
    final role = identityRole.toLowerCase();
    if (staffRole == AppConstants.roleOwner ||
        role == AppConstants.roleOwner ||
        role == 'gym_owner') {
      return AppConstants.roleOwner;
    }
    return AppConstants.roleReceptionist;
  }

  static Map<String, dynamic> _decodeJwtPayload(String token) {
    try {
      final parts = token.split('.');
      if (parts.length != 3) return const {};
      final decoded =
          utf8.decode(base64Url.decode(base64Url.normalize(parts[1])));
      final json = jsonDecode(decoded);
      return json is Map<String, dynamic> ? json : const {};
    } catch (_) {
      return const {};
    }
  }

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
