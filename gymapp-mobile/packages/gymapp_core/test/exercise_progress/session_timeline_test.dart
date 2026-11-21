import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';

import 'board_fixtures.dart';
import 'exercise_progress_view_test.dart' show pump;

void main() {
  testWidgets('sessions tab is the default: next session plan, latest open, older folded', (tester) async {
    String? opened;
    await pump(
      tester,
      ExerciseProgressView(
        board: trialBoard(),
        goal: ProgressGoalType.buildMuscle,
        isArabic: false,
        onOpenSession: (id) => opened = id,
      ),
    );

    // Next session card.
    expect(find.text('Your next session'), findsOneWidget);
    expect(find.text('Focus: Add 2.5 kg on bench'), findsNWidgets(2));
    expect(find.text('Your coach set 1 target for you'), findsOneWidget);
    expect(find.text('Bench press · 3 × 10 @ 65 kg'), findsOneWidget);

    // Latest session, expanded.
    expect(find.text('Session 3 · 8/11 · Push Day · Trial'), findsOneWidget);
    expect(find.text('1 new best'), findsOneWidget);
    expect(find.text('1 target hit'), findsOneWidget);
    expect(find.text('+2.5 kg'), findsOneWidget);
    expect(find.text('3 × 10,10,10 @ 62.5 kg'), findsOneWidget);
    expect(find.text('Great energy today'), findsOneWidget);
    expect(find.text('Your coach says'), findsOneWidget);

    // Older sessions folded to one line.
    expect(find.text('Session 2 · 3/11 · Pull Day · Trial'), findsOneWidget);
    expect(find.text('1 exercise'), findsNWidgets(2));
    expect(find.text('3 × 10 @ 50 kg'), findsNothing);

    await tester.tap(find.text('Session details'));
    expect(opened, 's3');
    expect(tester.takeException(), isNull);
  });

  testWidgets('tapping a folded session opens it; first time is marked', (tester) async {
    await pump(tester, ExerciseProgressView(board: trialBoard(), goal: null, isArabic: false));

    await tester.tap(find.text('Session 2 · 3/11 · Pull Day · Trial'));
    await tester.pumpAndSettle();

    expect(find.text('3 × 10 @ 50 kg'), findsOneWidget);
    expect(find.text('First time'), findsOneWidget);
  });

  testWidgets('switching to exercises shows the per-exercise board with the change since last time', (tester) async {
    await pump(tester, ExerciseProgressView(board: trialBoard(), goal: ProgressGoalType.buildMuscle, isArabic: false));

    await tester.tap(find.text('Exercises'));
    await tester.pumpAndSettle();

    expect(find.text('Push Day · 2'), findsOneWidget);
    expect(find.text('vs last time: +2.5 kg'), findsOneWidget);
    expect(find.text('Your next session'), findsNothing);
  });

  testWidgets('short trial shows one dot per free session', (tester) async {
    final json = trialBoardJson();
    (json['summary'] as Map)['trialSessionsTotal'] = 4;
    await pump(tester, ExerciseProgressView(board: ExerciseProgressModel.fromJson(json), goal: null, isArabic: false));

    expect(find.text('1 free session left'), findsOneWidget);
    expect(find.text('Free session 4 of 4'), findsOneWidget);
    expect(find.byType(LinearProgressIndicator), findsNothing);
  });

  testWidgets('Arabic: sessions tab right-to-left', (tester) async {
    await pump(tester, ExerciseProgressView(board: trialBoard(), goal: null, isArabic: true), arabic: true);

    expect(find.text('الحصص'), findsOneWidget);
    expect(find.text('مدربك يقول'), findsOneWidget);
    expect(find.text('+2.5 كجم'), findsOneWidget);
    expect(Directionality.of(tester.element(find.text('مدربك يقول'))), TextDirection.rtl);
    expect(tester.takeException(), isNull);
  });
}
