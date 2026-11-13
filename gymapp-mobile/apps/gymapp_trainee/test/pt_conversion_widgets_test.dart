import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/features/scheduling/presentation/widgets/pt_conversion_widgets.dart';

class _FakeFlags implements FeatureFlagService {
  final FeatureFlags flags;
  _FakeFlags(this.flags);

  @override
  FeatureFlags get current => flags;
  @override
  Stream<FeatureFlags> get changes => const Stream.empty();
  @override
  Future<FeatureFlags> load() async => flags;
  @override
  Future<FeatureFlags> refresh() async => flags;
  @override
  Future<void> clear() async {}
  @override
  void dispose() {}
}

FeatureFlags _ptWithout(String key) => FeatureFlags(
      values: {for (final k in FeatureFlagKeys.ptModuleDefaults) k: k != key},
      preset: 'pt_module',
      origin: FeatureFlagsOrigin.server,
    );

final _recap = TrialRecapModel(
  packageId: 'p1',
  trainerId: 't1',
  trainerName: 'Coach Omar',
  note: 'Strong start. Focus on form, 3x a week.',
  recommendedSessions: 12,
  sentAt: DateTime(2026, 11, 13),
  trialFinished: true,
);

Widget _host(FeatureFlags flags, Widget child, {Locale locale = const Locale('en')}) => ProviderScope(
      overrides: [featureFlagServiceProvider.overrideWithValue(_FakeFlags(flags))],
      child: MaterialApp(
        locale: locale,
        supportedLocales: const [Locale('en'), Locale('ar')],
        localizationsDelegates: const [
          GlobalMaterialLocalizations.delegate,
          GlobalWidgetsLocalizations.delegate,
          GlobalCupertinoLocalizations.delegate,
        ],
        home: Scaffold(body: SingleChildScrollView(child: child)),
      ),
    );

void main() {
  testWidgets('plan card shows the coach note, size and the interest button', (tester) async {
    await tester.pumpWidget(_host(FeatureFlags.ptDefaults(), TrialPlanCard(recap: _recap)));

    expect(find.text('Your plan from Coach Omar'), findsOneWidget);
    expect(find.text('Strong start. Focus on form, 3x a week.'), findsOneWidget);
    expect(find.text('Recommended: 12 sessions'), findsOneWidget);
    expect(find.text("I'm interested"), findsOneWidget);
  });

  testWidgets('interest button is hidden while pt.package_interest is off', (tester) async {
    await tester.pumpWidget(_host(_ptWithout(FeatureFlagKeys.ptPackageInterest), TrialPlanCard(recap: _recap)));

    expect(find.text('Recommended: 12 sessions'), findsOneWidget);
    expect(find.text("I'm interested"), findsNothing);
  });

  testWidgets('Arabic plan card renders right-to-left', (tester) async {
    await tester.pumpWidget(_host(FeatureFlags.ptDefaults(), TrialPlanCard(recap: _recap), locale: const Locale('ar')));

    expect(find.text('خطتك من Coach Omar'), findsOneWidget);
    expect(find.text('الباقة المقترحة: 12 حصة'), findsOneWidget);
    expect(find.text('مهتم بالباقة'), findsOneWidget);
    expect(Directionality.of(tester.element(find.byType(TrialPlanCard))), TextDirection.rtl);
  });
}
