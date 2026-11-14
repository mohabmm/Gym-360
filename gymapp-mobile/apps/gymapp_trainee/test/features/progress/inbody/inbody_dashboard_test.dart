import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/features/progress/presentation/providers/progress_providers.dart';
import 'package:gymapp_trainee/features/progress/presentation/screens/progress_dashboard_screen.dart';
import 'package:intl/date_symbol_data_local.dart';

class _Flags implements FeatureFlagService {
  @override
  final FeatureFlags current;

  _Flags(this.current);

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

FeatureFlags _withInBody() {
  final pt = FeatureFlags.ptDefaults();
  return FeatureFlags(
    values: {...pt.values, FeatureFlagKeys.progressInBody: true},
    preset: 'business',
    origin: FeatureFlagsOrigin.server,
  );
}

final _scan = BodyMetricEntryModel(
  id: 'e1',
  userId: 'm1',
  gymId: 'g1',
  date: '2026-11-10',
  weight: 80.1,
  weightUnit: 'Kg',
  bodyFatPercentage: 18.6,
  source: 'InBodyScan',
  loggedBy: 'm1',
  createdAt: DateTime(2026, 11, 10),
  composition: const BodyCompositionModel(
    heightCm: 178,
    skeletalMuscleMassKg: 35.2,
    bodyWaterKg: 45.2,
    skeletalMusclePercent: 43.9,
    bodyWaterPercent: 56.4,
    bmi: 25.3,
  ),
);

Future<void> _pump(WidgetTester tester, FeatureFlags flags) async {
  tester.view.physicalSize = const Size(1080, 6000);
  tester.view.devicePixelRatio = 2.5;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(ProviderScope(
    overrides: [
      featureFlagServiceProvider.overrideWithValue(_Flags(flags)),
      progressJourneyProvider.overrideWith(
          (ref) async => ProgressJourney.from(ProgressJourneyModel(userId: 'm1', entries: [_scan]))),
      assignedCoachProvider.overrideWithValue((id: 'coach-1', name: 'Ahmed')),
    ],
    child: const MaterialApp(
      locale: Locale('en'),
      supportedLocales: [Locale('ar'), Locale('en')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
      home: ProgressDashboardScreen(),
    ),
  ));
  await tester.pumpAndSettle();
}

void main() {
  setUpAll(() => initializeDateFormatting('en'));

  testWidgets('Business gym: latest InBody values and the scan action are shown', (tester) async {
    await _pump(tester, _withInBody());

    expect(find.text('Latest InBody'), findsOneWidget);
    expect(find.text('Scan InBody'), findsOneWidget);
    expect(find.text('178 cm'), findsOneWidget);
    expect(find.text('35.2 kg · 43.9%'), findsOneWidget);
    expect(find.text('45.2 kg · 56.4%'), findsOneWidget);
    expect(find.text('25.3'), findsOneWidget);
  });

  testWidgets('pt_module gym: no InBody card', (tester) async {
    await _pump(tester, FeatureFlags.ptDefaults());

    expect(find.text('Latest InBody'), findsNothing);
    expect(find.text('Scan InBody'), findsNothing);
  });
}
