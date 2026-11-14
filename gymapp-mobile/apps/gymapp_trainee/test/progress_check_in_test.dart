import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/core/providers.dart';
import 'package:gymapp_trainee/features/membership/presentation/providers/membership_providers.dart';
import 'package:gymapp_trainee/features/progress/presentation/providers/progress_providers.dart';
import 'package:gymapp_trainee/features/progress/presentation/screens/log_measurements_screen.dart';
import 'package:gymapp_trainee/features/progress/presentation/screens/progress_dashboard_screen.dart';
import 'package:intl/date_symbol_data_local.dart';

import 'integration/helpers/sample_payloads.dart';
import 'integration/helpers/test_api_client.dart';

class _PtFlags implements FeatureFlagService {
  @override
  FeatureFlags get current => FeatureFlags.ptDefaults();
  @override
  Stream<FeatureFlags> get changes => const Stream.empty();
  @override
  Future<FeatureFlags> load() async => current;
  @override
  Future<FeatureFlags> refresh() async => current;
  @override
  Future<void> clear() async {}
  @override
  void dispose() {}
}

class _SignedIn extends AuthNotifier {
  @override
  Future<AuthState> build() async => AuthState(
        status: AuthStatus.authenticated,
        user: UserModel.fromJson(Samples.user()),
        currentGymId: 'g1',
      );
}

final _today = DateTime.now();
DateTime _daysAgo(int d) => _today.subtract(Duration(days: d));

BodyMetricEntryModel _weighIn(DateTime day, double kg) => BodyMetricEntryModel(
      id: apiDate(day),
      userId: 'user-1',
      gymId: 'g1',
      date: apiDate(day),
      weight: kg,
      weightUnit: 'Kg',
      source: 'TrainerMeasured',
      loggedBy: 'coach-1',
      createdAt: day,
    );

ProgressJourney _journey(List<BodyMetricEntryModel> entries) => ProgressJourney.from(ProgressJourneyModel(
      userId: 'user-1',
      entries: entries,
      goal: const ProgressGoalModel(goal: ProgressGoalType.loseFat, targetWeight: 85),
    ));

SessionPackageModel _package({required bool trial, int remaining = 2, String status = 'Active'}) => SessionPackageModel(
      id: trial ? 'trial-1' : 'paid-1',
      trainerName: 'Ahmed',
      isTrial: trial,
      totalSessions: 20,
      remainingSessions: remaining,
      status: status,
    );

/// The member's history, packages and any extra provider overrides for one test.
class _Scenario {
  final List<BodyMetricEntryModel> entries;
  final List<SessionPackageModel> packages;
  final List<Override> extra;

  const _Scenario(this.entries, {this.packages = const [], this.extra = const []});
}

Future<void> _pump(WidgetTester tester, Widget home, _Scenario scenario) async {
  final _Scenario(:entries, :packages, :extra) = scenario;
  tester.view.physicalSize = const Size(1080, 6000);
  tester.view.devicePixelRatio = 2.5;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(ProviderScope(
    overrides: [
      featureFlagServiceProvider.overrideWithValue(_PtFlags()),
      progressJourneyProvider.overrideWith((ref) async => _journey(entries)),
      assignedCoachProvider.overrideWithValue((id: 'coach-1', name: 'Ahmed')),
      activePackagesProvider.overrideWith((ref) async => packages.where((p) => p.status == 'Active').toList()),
      allPackagesProvider.overrideWith((ref) async => packages),
      ...extra,
    ],
    child: MaterialApp(
      locale: const Locale('en'),
      supportedLocales: const [Locale('ar'), Locale('en')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
      home: home,
    ),
  ));
  await tester.pumpAndSettle();
}

void main() {
  setUpAll(() async {
    await initializeDateFormatting('ar');
    await initializeDateFormatting('en');
  });

  _progressTabTests();
  _logMeasurementsTests();
}

void _progressTabTests() {
  group('Progress tab', () {
    testWidgets('celebrates wins since day one with milestones', (tester) async {
      await _pump(tester, const ProgressDashboardScreen(),
          _Scenario([_weighIn(_daysAgo(30), 95), _weighIn(_daysAgo(3), 91)]));

      expect(find.text('Your wins since day one'), findsOneWidget);
      expect(find.text('↓ 4 kg'), findsWidgets);
      expect(find.text('2 check-ins'), findsOneWidget);
      expect(find.text('40% to target'), findsOneWidget);
      expect(find.byKey(const ValueKey('check-in-countdown')), findsOneWidget);
      expect(find.text('Next check-in in 11 days'), findsOneWidget);
    });

    testWidgets('two weeks after the last check-in, prompts to log now', (tester) async {
      await _pump(tester, const ProgressDashboardScreen(),
          _Scenario([_weighIn(_daysAgo(40), 95), _weighIn(_daysAgo(15), 93)]));

      expect(find.byKey(const ValueKey('check-in-due')), findsOneWidget);
      expect(find.text('Time for your check-in!'), findsOneWidget);
      expect(find.widgetWithText(FilledButton, 'Log now'), findsOneWidget);
    });

    testWidgets('trial nearly over: results + "continue with coach" + sessions left', (tester) async {
      await _pump(tester, const ProgressDashboardScreen(),
          _Scenario([_weighIn(_daysAgo(24), 95), _weighIn(_daysAgo(2), 92.9)], packages: [_package(trial: true, remaining: 2)]));

      expect(find.byKey(const ValueKey('progress-conversion')), findsOneWidget);
      expect(find.text("You're getting results 💪"), findsOneWidget);
      expect(find.text('in 24 days'), findsOneWidget);
      expect(find.text('2 free sessions left'), findsOneWidget);
      expect(find.text('Continue with Coach Ahmed'), findsOneWidget);
    });

    testWidgets('no conversion card with a paid package or early in the trial', (tester) async {
      await _pump(tester, const ProgressDashboardScreen(),
          _Scenario([_weighIn(_daysAgo(24), 95), _weighIn(_daysAgo(2), 93)], packages: [_package(trial: true, remaining: 12)]));
      expect(find.byKey(const ValueKey('progress-conversion')), findsNothing);

      await _pump(tester, const ProgressDashboardScreen(),
          _Scenario([_weighIn(_daysAgo(24), 95), _weighIn(_daysAgo(2), 93)], packages: [_package(trial: true, remaining: 0, status: 'Exhausted'), _package(trial: false, remaining: 8)]));
      expect(find.byKey(const ValueKey('progress-conversion')), findsNothing);
    });

    testWidgets('finished trial with no progress yet: momentum message', (tester) async {
      await _pump(tester, const ProgressDashboardScreen(),
          _Scenario([_weighIn(_daysAgo(20), 95)], packages: [_package(trial: true, remaining: 0, status: 'Exhausted')]));

      expect(find.text("Don't lose your momentum 🔥"), findsOneWidget);
      expect(find.text('2 free sessions left'), findsNothing);
    });
  });
}

void _logMeasurementsTests() {
  group('Log measurements', () {
    testWidgets('member check-in is sent as self-reported and celebrated', (tester) async {
      final t = buildTestApiClient();
      t.adapter.onPost(ApiEndpoints.bodyMetrics, respond: (_) => t.adapter.created('entry-1'));
      await _pump(
        tester,
        Builder(
          builder: (context) => Scaffold(
            body: TextButton(
              onPressed: () => Navigator.of(context)
                  .push(MaterialPageRoute<void>(builder: (_) => const LogMeasurementsScreen())),
              child: const Text('progress'),
            ),
          ),
        ),
        _Scenario([_weighIn(_daysAgo(14), 92)], extra: [
          apiClientProvider.overrideWithValue(t.apiClient),
          authProvider.overrideWith(_SignedIn.new),
        ]),
      );
      await tester.tap(find.text('progress'));
      await tester.pumpAndSettle();

      expect(find.textContaining('Last: 92 kg'), findsOneWidget);
      await tester.enterText(find.byKey(const ValueKey('metric-weight')), '91.2');
      await tester.enterText(find.byKey(const ValueKey('metric-waist')), '88');
      await tester.pump();
      await tester.tap(find.byKey(const ValueKey('save-measurements')));
      await tester.pumpAndSettle();

      final body = t.adapter.recorded.firstWhere((r) => r.method == 'POST' && r.path == ApiEndpoints.bodyMetrics).bodyAsMap!;
      expect(body['source'], 'SelfReported');
      expect(body['userId'], 'user-1');
      expect(body['loggedBy'], 'user-1');
      expect(body['date'], apiDate(_today));
      expect(body['weight'], 91.2);
      expect((body['measurements'] as Map)['waist'], 88.0);

      expect(find.text('Nice! 1 win since last time'), findsOneWidget);
      await tester.tap(find.text('Done'));
      await tester.pumpAndSettle();
      expect(find.text('progress'), findsOneWidget);
    });
  });
}
