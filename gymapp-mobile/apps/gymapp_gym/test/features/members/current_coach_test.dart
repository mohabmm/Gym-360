import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_gym/features/members/presentation/widgets/current_coach.dart';

/// One coach at a time: "Assign Trainer" is replaced by the current coach while the
/// member still has sessions left on a package that has not expired.
void main() {
  final now = DateTime(2026, 11, 14, 12);

  SessionPackageModel pkg({String status = 'Active', int left = 4, DateTime? expiresAt}) =>
      SessionPackageModel.fromJson({
        'id': 'p1',
        'trainerId': 'coach-1',
        'trainerName': 'Omar Coach',
        'memberId': 'm1',
        'remainingSessions': left,
        'totalSessions': 8,
        'status': status,
        if (expiresAt != null) 'expiresAt': expiresAt.toIso8601String(),
      });

  test('an active package with sessions left is the current coach', () {
    expect(currentCoachPackage([pkg()], now: now)?.trainerId, 'coach-1');
  });

  test('no packages means no coach, so a coach can be assigned', () {
    expect(currentCoachPackage(const [], now: now), isNull);
  });

  test('a used-up, expired or exhausted package does not block a new coach', () {
    expect(currentCoachPackage([pkg(left: 0)], now: now), isNull);
    expect(currentCoachPackage([pkg(status: 'Exhausted')], now: now), isNull);
    expect(currentCoachPackage([pkg(expiresAt: now.subtract(const Duration(days: 1)))], now: now), isNull);
  });

  test('a package that expires later still counts', () {
    expect(currentCoachPackage([pkg(expiresAt: now.add(const Duration(days: 5)))], now: now), isNotNull);
  });
}
