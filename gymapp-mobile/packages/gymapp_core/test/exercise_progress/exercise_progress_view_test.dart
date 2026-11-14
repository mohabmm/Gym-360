import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'board_fixtures.dart';

Future<void> pump(WidgetTester tester, Widget child, {bool arabic = false}) async {
  tester.view.physicalSize = const Size(432 * 2.5, 10000);
  tester.view.devicePixelRatio = 2.5;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(MaterialApp(
    locale: Locale(arabic ? 'ar' : 'en'),
    supportedLocales: const [Locale('ar'), Locale('en')],
    localizationsDelegates: GlobalMaterialLocalizations.delegates,
    home: Scaffold(body: SingleChildScrollView(child: child)),
  ));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('member: goal story, trial progress, day chips and exercise cards', (tester) async {
    await pump(tester, ExerciseProgressView(board: trialBoard(), goal: ProgressGoalType.buildMuscle, isArabic: false));

    expect(find.text('Goal: build muscle — we follow your working weight'), findsOneWidget);
    expect(find.text('Stronger on 1 of 1 exercises'), findsOneWidget);
    expect(find.text('Trial sessions: 3 of 20'), findsOneWidget);
    expect(find.byType(LinearProgressIndicator), findsOneWidget);
    expect(find.text('Push Day · 2'), findsOneWidget);
    expect(find.text('Bench press'), findsOneWidget);
    expect(find.text('3 × 10,10,10 @ 62.5 kg'), findsOneWidget);
    expect(find.text('New best'), findsOneWidget);
    expect(find.text("Hit coach's target"), findsOneWidget);
    expect(find.text('Next target: 3 × 10 @ 65 kg'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('picking a day shows only its exercises', (tester) async {
    await pump(tester, ExerciseProgressView(board: trialBoard(), goal: null, isArabic: false));

    await tester.tap(find.text('Pull Day · 1'));
    await tester.pumpAndSettle();

    expect(find.text('Barbell row'), findsOneWidget);
    expect(find.text('Bench press'), findsNothing);
  });

  testWidgets('tapping an exercise opens its history with the coach note', (tester) async {
    await pump(tester, ExerciseProgressView(board: trialBoard(), goal: ProgressGoalType.loseFat, isArabic: false));

    await tester.tap(find.text('Bench press'));
    await tester.pumpAndSettle();

    expect(find.text('Coach note: Elbows in'), findsOneWidget);
    expect(find.textContaining('Session 1 · 1/11 · Push Day · Trial'), findsOneWidget);
  });

  testWidgets('Arabic: right-to-left with Arabic names and labels', (tester) async {
    await pump(
      tester,
      ExerciseProgressView(board: trialBoard(), goal: ProgressGoalType.buildMuscle, isArabic: true),
      arabic: true,
    );

    expect(find.text('ضغط صدر'), findsOneWidget);
    expect(find.text('الحصص التجريبية: 3 من 20'), findsOneWidget);
    expect(find.text('رقم قياسي'), findsOneWidget);
    expect(find.text('يوم الدفع · 2'), findsOneWidget);
    expect(Directionality.of(tester.element(find.text('ضغط صدر'))), TextDirection.rtl);
    expect(tester.takeException(), isNull);
  });

  testWidgets('paid member: no trial bar; empty board shows the footer', (tester) async {
    await pump(tester, ExerciseProgressView(board: trialBoard(paid: true), goal: null, isArabic: false));
    expect(find.byType(LinearProgressIndicator), findsNothing);

    await pump(
      tester,
      ExerciseProgressView(
        board: ExerciseProgressModel.fromJson(const {}),
        goal: null,
        isArabic: false,
        footer: const Text('keep training'),
      ),
    );
    expect(find.text('No numbers yet'), findsOneWidget);
    expect(find.text('keep training'), findsOneWidget);
  });
}
