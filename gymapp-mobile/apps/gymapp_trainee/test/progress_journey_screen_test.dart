import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/features/progress/presentation/providers/progress_providers.dart';
import 'package:gymapp_trainee/features/progress/presentation/screens/progress_dashboard_screen.dart';
import 'package:intl/date_symbol_data_local.dart';

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

BodyMetricEntryModel _entry(String date, double weight, String source) => BodyMetricEntryModel(
      id: date,
      userId: 'm1',
      gymId: 'g1',
      date: date,
      weight: weight,
      weightUnit: 'Kg',
      source: source,
      loggedBy: source == 'TrainerMeasured' ? 'coach-1' : 'm1',
      createdAt: DateTime.parse(date),
    );

Future<void> _pump(WidgetTester tester, ProgressJourneyModel model, {String locale = 'en'}) async {
  tester.view.physicalSize = const Size(1080, 6000);
  tester.view.devicePixelRatio = 2.5;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(ProviderScope(
    overrides: [
      featureFlagServiceProvider.overrideWithValue(_PtFlags()),
      progressJourneyProvider.overrideWith((ref) async => ProgressJourney.from(model)),
      assignedCoachProvider.overrideWithValue((id: 'coach-1', name: 'Ahmed')),
    ],
    child: MaterialApp(
      locale: Locale(locale),
      supportedLocales: const [Locale('ar'), Locale('en')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
      home: const ProgressDashboardScreen(),
    ),
  ));
  await tester.pumpAndSettle();
}

void main() {
  setUpAll(() async {
    await initializeDateFormatting('ar');
    await initializeDateFormatting('en');
  });

  testWidgets('PT gym: progress tab shows day one vs now, with no streak/check-in/photo sections', (tester) async {
    await _pump(
      tester,
      ProgressJourneyModel(
        userId: 'm1',
        entries: [_entry('2026-09-01', 95, 'TrainerMeasured'), _entry('2026-11-01', 91, 'TrainerMeasured')],
        goal: const ProgressGoalModel(goal: ProgressGoalType.loseFat, targetWeight: 85),
      ),
    );

    expect(find.text('Since day one'), findsOneWidget);
    expect(find.text('Lose fat'), findsOneWidget);
    expect(find.text('Coach Ahmed'), findsNWidgets(2));
    expect(find.text('Log measurements'), findsOneWidget);
    expect(find.text('Current Streak'), findsNothing);
    expect(find.text('Progress Photos'), findsNothing);
    expect(find.text('Check-In Streak'), findsNothing);
  });

  testWidgets('new member in Arabic sees the coach-measures-first empty state', (tester) async {
    await _pump(tester, const ProgressJourneyModel(userId: 'm1', entries: []), locale: 'ar');

    expect(find.text('لم تبدأ القياسات بعد'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
}
