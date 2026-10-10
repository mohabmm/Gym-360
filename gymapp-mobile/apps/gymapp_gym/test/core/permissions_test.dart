import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_gym/core/permissions.dart';

void main() {
  group('StaffPermissions - owner', () {
    test('has every permission', () {
      for (final permission in StaffPermission.values) {
        expect(
          StaffPermissions.hasPermission(AppConstants.roleOwner, permission),
          isTrue,
          reason: 'Owner must have $permission',
        );
      }
    });
  });

  group('StaffPermissions - manager', () {
    test('has day-to-day ops permissions', () {
      const granted = [
        StaffPermission.viewDashboard,
        StaffPermission.viewAnalytics,
        StaffPermission.addMember,
        StaffPermission.editMember,
        StaffPermission.freezeMember,
        StaffPermission.approveTrainer,
        StaffPermission.createClass,
        StaffPermission.cancelClass,
        StaffPermission.recordPayment,
        StaffPermission.viewRevenue,
        StaffPermission.kioskMode,
        StaffPermission.manualCheckIn,
        StaffPermission.createAnnouncement,
        StaffPermission.moderateTestimonials,
      ];
      for (final p in granted) {
        expect(
          StaffPermissions.hasPermission(AppConstants.roleManager, p),
          isTrue,
          reason: 'Manager should have $p',
        );
      }
    });

    test('manages packages, staff and branding per the docs matrix', () {
      const granted = [
        StaffPermission.managePackageTemplates,
        StaffPermission.addStaff,
        StaffPermission.editStaff,
        StaffPermission.editBranding,
        StaffPermission.editGymProfile,
      ];
      for (final p in granted) {
        expect(
          StaffPermissions.hasPermission(AppConstants.roleManager, p),
          isTrue,
          reason: 'Manager should have $p',
        );
      }
    });

    test('cannot remove members', () {
      expect(
        StaffPermissions.hasPermission(
            AppConstants.roleManager, StaffPermission.deleteMember),
        isFalse,
      );
    });
  });

  group('StaffPermissions - receptionist', () {
    test('has front-desk permissions', () {
      const granted = [
        StaffPermission.viewDashboard,
        StaffPermission.viewMembers,
        StaffPermission.addMember,
        StaffPermission.editMember,
        StaffPermission.freezeMember,
        StaffPermission.viewCheckInLog,
        StaffPermission.manualCheckIn,
        StaffPermission.kioskMode,
        StaffPermission.recordPayment,
        StaffPermission.viewPayments,
        StaffPermission.assignPackage,
        StaffPermission.viewClasses,
        StaffPermission.viewAnnouncements,
      ];
      for (final p in granted) {
        expect(
          StaffPermissions.hasPermission(AppConstants.roleReceptionist, p),
          isTrue,
          reason: 'Receptionist should have $p',
        );
      }
    });

    test('is blocked from management actions', () {
      const denied = [
        StaffPermission.deleteMember,
        StaffPermission.approveTrainer,
        StaffPermission.createClass,
        StaffPermission.cancelClass,
        StaffPermission.createAnnouncement,
        StaffPermission.viewAnalytics,
        StaffPermission.viewRevenue,
        StaffPermission.managePackageTemplates,
        StaffPermission.viewStaff,
        StaffPermission.addStaff,
        StaffPermission.editGymProfile,
        StaffPermission.editBranding,
        StaffPermission.editSessionPolicies,
        StaffPermission.viewTrainerRatings,
        StaffPermission.moderateTestimonials,
      ];
      for (final p in denied) {
        expect(
          StaffPermissions.hasPermission(AppConstants.roleReceptionist, p),
          isFalse,
          reason: 'Receptionist should NOT have $p',
        );
      }
    });
  });

  group('StaffPermissions.roleFromAccessToken', () {
    String token(Map<String, dynamic> claims) {
      String part(Object o) =>
          base64Url.encode(utf8.encode(jsonEncode(o))).replaceAll('=', '');
      return '${part({'alg': 'HS256'})}.${part(claims)}.sig';
    }

    test('reads the per-gym tier from staffSubRole', () {
      expect(
        StaffPermissions.roleFromAccessToken(
            token({'staffRole': 'staff', 'staffSubRole': 'Receptionist'}),
            'staff'),
        AppConstants.roleReceptionist,
      );
      expect(
        StaffPermissions.roleFromAccessToken(
            token({'staffRole': 'staff', 'staffSubRole': 'Manager'}), 'staff'),
        AppConstants.roleManager,
      );
    });

    test('owner principals resolve to owner', () {
      expect(
        StaffPermissions.roleFromAccessToken(
            token({'staffRole': 'owner'}), 'owner'),
        AppConstants.roleOwner,
      );
    });

    test('staff without a readable tier get least privilege', () {
      expect(
        StaffPermissions.roleFromAccessToken(
            token({'staffRole': 'staff'}), 'staff'),
        AppConstants.roleReceptionist,
      );
      expect(
        StaffPermissions.roleFromAccessToken('not-a-jwt', 'staff'),
        AppConstants.roleReceptionist,
      );
    });
  });

  group('StaffPermissions - unknown role', () {
    test('returns false for every permission', () {
      for (final p in StaffPermission.values) {
        expect(StaffPermissions.hasPermission('ghost', p), isFalse);
      }
    });

    test('permissionsForRole returns empty set', () {
      expect(StaffPermissions.permissionsForRole('ghost'), isEmpty);
    });
  });

  group('StaffPermissions - case insensitivity', () {
    test('accepts upper-case role names', () {
      expect(
        StaffPermissions.hasPermission('OWNER', StaffPermission.addStaff),
        isTrue,
      );
      expect(
        StaffPermissions.hasPermission('Manager', StaffPermission.recordPayment),
        isTrue,
      );
    });
  });

  group('StaffPermissions.roleDisplayName', () {
    test('localized labels', () {
      expect(
        StaffPermissions.roleDisplayName(AppConstants.roleOwner, isArabic: true),
        'مالك',
      );
      expect(
        StaffPermissions.roleDisplayName(AppConstants.roleOwner, isArabic: false),
        'Owner',
      );
      expect(
        StaffPermissions.roleDisplayName(AppConstants.roleManager, isArabic: false),
        'Manager',
      );
      expect(
        StaffPermissions.roleDisplayName(
            AppConstants.roleReceptionist,
            isArabic: false),
        'Receptionist',
      );
    });

    test('unknown role echoes back input', () {
      expect(StaffPermissions.roleDisplayName('intern'), 'intern');
    });
  });
}
