// Kiosk + permission sanity smoke tests.
// Detailed permission matrix lives in test/core/permissions_test.dart.
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_gym/core/permissions.dart';

void main() {
  test('kiosk mode is allowed for all staff roles', () {
    for (final role in [
      AppConstants.roleOwner,
      AppConstants.roleManager,
      AppConstants.roleReceptionist,
    ]) {
      expect(
        StaffPermissions.hasPermission(role, StaffPermission.kioskMode),
        isTrue,
        reason: '$role must be able to run kiosk mode',
      );
    }
  });

  test('owner and manager can add staff, receptionist cannot', () {
    expect(
      StaffPermissions.hasPermission(
          AppConstants.roleOwner, StaffPermission.addStaff),
      isTrue,
    );
    expect(
      StaffPermissions.hasPermission(
          AppConstants.roleManager, StaffPermission.addStaff),
      isTrue,
    );
    expect(
      StaffPermissions.hasPermission(
          AppConstants.roleReceptionist, StaffPermission.addStaff),
      isFalse,
    );
  });
}
