import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/features/membership/presentation/providers/membership_providers.dart';
import 'package:gymapp_trainee/features/progress/presentation/providers/exercise_progress_providers.dart';
import 'package:gymapp_trainee/features/progress/presentation/providers/progress_providers.dart';
import 'package:gymapp_trainee/features/progress/presentation/screens/exercise_progress_screen.dart';
import 'package:gymapp_trainee/features/scheduling/presentation/widgets/pt_conversion_widgets.dart';

import 'exercise_progress_fixtures.dart';

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

ProgressJourney _journeyWithGoal(ProgressGoalType goal) => ProgressJourney.from(
      ProgressJourneyModel(userId: 'm1', entries: const [], goal: ProgressGoalModel(goal: goal)),
    );

Future<void> _pump(WidgetTester tester, ExerciseProgressModel board,
    {ProgressGoalType goal = ProgressGoalType.buildMuscle, String locale = 'en'}) async {
  tester.view.physicalSize = const Size(1080, 6000);
  tester.view.devicePixelRatio = 2.5;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(ProviderScope(
    overrides: [
      featureFlagServiceProvider.overrideWithValue(_PtFlags()),
      myExerciseProgressProvider.overrideWith((ref) async => board),
      progressJourneyProvider.overrideWith((ref) async => _journeyWithGoal(goal)),
      finishedTrialProvider.overrideWithValue(null),
      myTrialRecapProvider.overrideWith((ref) async => null),
      activePackagesProvider.overrideWith((ref) async => const <SessionPackageModel>[]),
    ],
    child: MaterialApp(
      locale: Locale(locale),
      supportedLocales: const [Locale('ar'), Locale('en')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
      home: const ExerciseProgressScreen(),
    ),
  ));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('trial member sees the coach numbers framed by their goal and the keep-training line', (tester) async {
    await _pump(tester, trialBoard());

    expect(find.text('Exercise progress'), findsOneWidget);
    expect(find.text('Goal: build muscle — we follow your working weight'), findsOneWidget);
    expect(find.text('Stronger on 1 of 1 exercises'), findsOneWidget);
    expect(find.text('Bench press'), findsOneWidget);
    expect(find.textContaining('Keep training with your coach'), findsOneWidget);
  });

  testWidgets('lose-fat goal leads with total work', (tester) async {
    await _pump(tester, trialBoard(), goal: ProgressGoalType.loseFat);

    expect(find.textContaining('More work on 1 of 1 exercises'), findsOneWidget);
    expect(find.textContaining('Total work:'), findsOneWidget);
  });

  testWidgets('paid member: no trial footer', (tester) async {
    await _pump(tester, trialBoard(paid: true));

    expect(find.textContaining('Keep training with your coach'), findsNothing);
  });

  testWidgets('Arabic screen renders right-to-left', (tester) async {
    await _pump(tester, trialBoard(), locale: 'ar');

    expect(find.text('تقدم التمارين'), findsOneWidget);
    expect(Directionality.of(tester.element(find.text('ضغط صدر'))), TextDirection.rtl);
    expect(tester.takeException(), isNull);
  });
}
