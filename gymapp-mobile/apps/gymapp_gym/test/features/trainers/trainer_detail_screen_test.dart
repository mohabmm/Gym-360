import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_gym/features/trainers/presentation/providers/trainers_provider.dart';
import 'package:gymapp_gym/features/trainers/presentation/screens/trainer_detail_screen.dart';

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

const _trainer = UserModel(id: 't-1', email: 'omar@gym.eg', firstName: 'Omar', lastName: 'Ali', role: 'trainer');

Widget _app(FeatureFlags flags, {List<TrainerRatingModel> ratings = const []}) => ProviderScope(
      overrides: [
        featureFlagServiceProvider.overrideWithValue(_Flags(flags)),
        trainerDetailProvider('t-1').overrideWith((ref) async => _trainer),
        trainerRatingsProvider('t-1').overrideWith((ref) async => ratings),
      ],
      child: const MaterialApp(
        locale: Locale('en'),
        supportedLocales: [Locale('ar'), Locale('en')],
        localizationsDelegates: GlobalMaterialLocalizations.delegates,
        home: TrainerDetailScreen(trainerId: 't-1'),
      ),
    );

void main() {
  testWidgets('pt_module (coach ratings off): no ratings action that would bounce to the dashboard', (tester) async {
    await tester.pumpWidget(_app(FeatureFlags.ptDefaults()));
    await tester.pumpAndSettle();

    expect(find.text('Omar Ali'), findsOneWidget);
    expect(find.byIcon(Icons.star_rate), findsNothing);
    expect(find.text('Avg Rating'), findsNothing);
  });

  testWidgets('coach ratings on: ratings action and summary are shown', (tester) async {
    final flags = FeatureFlags(
      values: {for (final k in FeatureFlagKeys.ptModuleDefaults) k: true, FeatureFlagKeys.ptCoachRatings: true},
      preset: 'pt_module',
      origin: FeatureFlagsOrigin.server,
    );
    await tester.pumpWidget(_app(flags, ratings: [
      const TrainerRatingModel(id: 'r1', trainerId: 't-1', memberId: 'm1', sessionId: 's1', stars: 4),
      const TrainerRatingModel(id: 'r2', trainerId: 't-1', memberId: 'm2', sessionId: 's2', stars: 5),
    ]));
    await tester.pumpAndSettle();

    expect(find.byIcon(Icons.star_rate), findsOneWidget);
    expect(find.text('4.5'), findsOneWidget);
  });
}
