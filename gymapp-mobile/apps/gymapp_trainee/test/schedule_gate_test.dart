import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/features/membership/presentation/providers/membership_providers.dart';
import 'package:gymapp_trainee/features/scheduling/presentation/providers/scheduling_providers.dart';
import 'package:gymapp_trainee/features/scheduling/presentation/widgets/awaiting_coach_view.dart';

/// The member app shows no sessions until the desk assigns a coach and sessions.
void main() {
  final now = DateTime(2026, 11, 13, 12);

  SessionPackageModel package({String status = 'Active', String trainerId = 'coach-1', int total = 6}) =>
      SessionPackageModel(id: 'p1', status: status, trainerId: trainerId, totalSessions: total, remainingSessions: total);

  SessionModel session() => SessionModel(
        id: 's1',
        gymId: 'g1',
        trainerId: 'coach-1',
        memberId: 'm1',
        scheduledAt: now,
        durationMinutes: 60,
        sessionType: 'PersonalTraining',
        status: 'Completed',
        createdAt: now,
      );

  group('hasAssignedCoach', () {
    test('a new member with nothing assigned has no coach', () {
      expect(hasAssignedCoach(const [], const []), isFalse);
    });

    test('an active package with a coach and sessions counts as assigned', () {
      expect(hasAssignedCoach([package()], const []), isTrue);
    });

    test('a package without a coach or without sessions does not count', () {
      expect(hasAssignedCoach([package(trainerId: '')], const []), isFalse);
      expect(hasAssignedCoach([package(total: 0)], const []), isFalse);
      expect(hasAssignedCoach([package(status: 'Exhausted')], const []), isFalse);
    });

    test('session history keeps the screen open after the package ran out', () {
      expect(hasAssignedCoach(const [], [session()]), isTrue);
    });
  });

  group('CoachAssignedGate', () {
    Future<void> pump(WidgetTester tester, List<SessionPackageModel> packages) => tester.pumpWidget(
          ProviderScope(
            overrides: [
              activePackagesProvider.overrideWith((ref) async => packages),
              mySessionsAllProvider.overrideWith((ref) async => const <SessionModel>[]),
            ],
            child: const MaterialApp(
              home: CoachAssignedGate(child: Text('sessions-screen')),
            ),
          ),
        );

    testWidgets('shows the waiting message and no sessions before assignment', (tester) async {
      await pump(tester, const []);
      await tester.pumpAndSettle();

      expect(find.byType(AwaitingCoachView), findsOneWidget);
      expect(find.text('No coach assigned yet'), findsOneWidget);
      expect(find.text('sessions-screen'), findsNothing);
    });

    testWidgets('shows the sessions screen once the desk assigned a coach', (tester) async {
      await pump(tester, [package()]);
      await tester.pumpAndSettle();

      expect(find.text('sessions-screen'), findsOneWidget);
      expect(find.byType(AwaitingCoachView), findsNothing);
    });
  });
}
