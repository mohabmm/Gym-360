import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainer/features/clients/presentation/providers/client_providers.dart';
import 'package:gymapp_trainer/features/clients/presentation/screens/record_measurements_screen.dart';

Future<void> _pump(WidgetTester tester, ProgressGoalModel? goal) async {
  await tester.pumpWidget(ProviderScope(
    overrides: [
      clientJourneyProvider('c1').overrideWith(
        (ref) async => ProgressJourney.from(ProgressJourneyModel(userId: 'c1', entries: const [], goal: goal)),
      ),
    ],
    child: const MaterialApp(
      locale: Locale('en'),
      supportedLocales: [Locale('ar'), Locale('en')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
      home: RecordMeasurementsScreen(clientId: 'c1'),
    ),
  ));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('first session (no goal yet): coach picks a goal, then a target field appears', (tester) async {
    await _pump(tester, null);

    expect(find.text('Goal'), findsOneWidget);
    expect(find.text('Target weight (kg)'), findsNothing);
    await tester.tap(find.widgetWithText(ChoiceChip, 'Lose fat'));
    await tester.pumpAndSettle();
    expect(find.text('Target weight (kg)'), findsOneWidget);
  });

  testWidgets('client already has a goal: the goal step is not shown', (tester) async {
    await _pump(tester, const ProgressGoalModel(goal: ProgressGoalType.buildMuscle));

    expect(find.widgetWithText(ChoiceChip, 'Lose fat'), findsNothing);
    expect(find.text('Weight'), findsOneWidget);
  });
}
