import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/features/scheduling/presentation/providers/scheduling_providers.dart';

void main() {
  final now = DateTime(2026, 6, 13, 12, 0);

  SessionModel session(String status, DateTime when) => SessionModel(
        id: 'id-${status}-${when.millisecondsSinceEpoch}',
        gymId: 'g1',
        trainerId: 't1',
        memberId: 'm1',
        scheduledAt: when,
        durationMinutes: 60,
        sessionType: 'PersonalTraining',
        status: status,
        createdAt: now,
      );

  group('computeBookableSessions', () {
    test('no sessions -> full remaining', () {
      expect(computeBookableSessions(4, const [], now), 4);
    });

    test('upcoming pending reserves a slot', () {
      final s = [session('Pending', now.add(const Duration(days: 1)))];
      expect(computeBookableSessions(4, s, now), 3);
    });

    test('REGRESSION: past-dated stuck Pending does NOT reserve a slot', () {
      // The June-1 session still Pending on June-13 must not lock a paid slot.
      final s = [session('Pending', now.subtract(const Duration(days: 12)))];
      expect(computeBookableSessions(4, s, now), 4);
    });

    test('past confirmed (never completed) also does not reserve', () {
      final s = [session('Confirmed', now.subtract(const Duration(days: 2)))];
      expect(computeBookableSessions(4, s, now), 4);
    });

    test('in-progress always reserves, even if start time is past', () {
      final s = [session('InProgress', now.subtract(const Duration(minutes: 20)))];
      expect(computeBookableSessions(4, s, now), 3);
    });

    test('mixed: 1 future pending + 1 stale past pending => only 1 reserved', () {
      final s = [
        session('Pending', now.add(const Duration(days: 1))),
        session('Pending', now.subtract(const Duration(days: 12))),
      ];
      expect(computeBookableSessions(4, s, now), 3);
    });

    test('completed/cancelled never reserve', () {
      final s = [
        session('Completed', now.add(const Duration(days: 1))),
        session('CancelledByMember', now.add(const Duration(days: 1))),
      ];
      expect(computeBookableSessions(4, s, now), 4);
    });

    test('never goes negative', () {
      final s = List.generate(
          6, (_) => session('Pending', now.add(const Duration(days: 1))));
      expect(computeBookableSessions(4, s, now), 0);
    });
  });
}
