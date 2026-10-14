import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/features/home/presentation/providers/home_providers.dart';
import 'package:gymapp_trainee/features/scheduling/presentation/providers/scheduling_providers.dart';

void main() {
  SessionModel session(String id) => SessionModel(
        id: id,
        gymId: 'g1',
        trainerId: 't1',
        memberId: 'm1',
        scheduledAt: DateTime(2026, 10, 16, 10, 15),
        durationMinutes: 60,
        sessionType: 'PersonalTraining',
        status: 'Pending',
        createdAt: DateTime(2026, 10, 14),
      );

  test(
      'REGRESSION: booking from Schedule refreshes the Home next-session card',
      () async {
    // Found in the MobAI run: Home kept "no upcoming session" after a booking
    // because it cached its own copy of /scheduling/sessions/upcoming, while
    // the booking flow only invalidates the Schedule tab's provider.
    var serverSessions = <SessionModel>[];
    final container = ProviderContainer(overrides: [
      upcomingSessionsListProvider.overrideWith((ref) async => serverSessions),
    ]);
    addTearDown(container.dispose);
    final sub = container.listen(upcomingSessionsProvider, (_, __) {});
    addTearDown(sub.close);

    expect(await container.read(upcomingSessionsProvider.future), isEmpty);

    // The trainee books a session; BookSessionScreen invalidates the list.
    serverSessions = [session('s1')];
    container.invalidate(upcomingSessionsListProvider);

    final home = await container.read(upcomingSessionsProvider.future);
    expect(home.map((s) => s.id), ['s1']);
  });
}
