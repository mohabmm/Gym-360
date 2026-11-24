import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_gym/core/providers.dart';
import 'package:gymapp_gym/features/members/presentation/widgets/monthly_programs_card.dart';
import 'package:intl/date_symbol_data_local.dart';

import '../../integration/helpers/test_api_client.dart';

Map<String, dynamic> _coach({required bool unlocked, String length = 'OneWeek', Map<String, dynamic>? plan}) => {
      'traineeId': 'm1',
      'coaches': [
        {'trainerId': 'c1', 'trainerName': 'كابتن محمود', 'unlocked': unlocked, 'length': length, 'changedAt': null, 'limitedPlan': plan},
      ],
    };

Future<TestApiClient> _pump(WidgetTester tester, Map<String, dynamic> Function() body) async {
  final api = buildTestApiClient();
  api.adapter.onGet(ApiEndpoints.planUnlocks, respond: (_) => api.adapter.ok(body()));
  await tester.pumpWidget(ProviderScope(
    overrides: [apiClientProvider.overrideWithValue(api.apiClient)],
    child: const MaterialApp(
      locale: Locale('ar'),
      supportedLocales: [Locale('ar'), Locale('en')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
      home: Scaffold(body: MonthlyProgramsCard(memberId: 'm1', isArabic: true)),
    ),
  ));
  await tester.pumpAndSettle();
  return api;
}

void main() {
  setUpAll(() async => initializeDateFormatting('ar'));

  testWidgets('pt.starter_plan: the member stopped coming, so the desk unlocks 30 days for the coach', (tester) async {
    var unlocked = false;
    final api = await _pump(tester, () => _coach(
          unlocked: unlocked,
          length: unlocked ? 'ThirtyDays' : 'OneWeek',
          plan: {'planId': 'p1', 'limit': 'Starter', 'startDate': '2026-11-20', 'endsOn': '2026-11-22', 'hasEnded': true},
        ));
    api.adapter.onPut(ApiEndpoints.planUnlocks, respond: (_) {
      unlocked = true;
      return api.adapter.ok({'trainerId': 'c1', 'trainerName': '', 'unlocked': true});
    });

    expect(find.text('كابتن محمود'), findsOneWidget);
    expect(find.text('خلصت خطة البداية ٣ أيام'), findsOneWidget);

    await tester.tap(find.byKey(const ValueKey('unlock-month-c1')));
    for (var i = 0; i < 5; i++) {
      await tester.pump(const Duration(milliseconds: 50));
    }
    await tester.pumpAndSettle();

    final put = api.adapter.recorded.singleWhere((r) => r.method == 'PUT');
    expect(put.bodyAsMap, {'trainerId': 'c1', 'traineeId': 'm1', 'unlocked': true, 'length': 'ThirtyDays'});
    expect(find.text('مفتوح: برنامج ٣٠ يوم – المدرب يقدر يدّيه'), findsOneWidget);
    expect(find.byKey(const ValueKey('lock-c1')), findsOneWidget);
  });

  testWidgets('a member without a coach shows nothing', (tester) async {
    await _pump(tester, () => {'traineeId': 'm1', 'coaches': const []});

    expect(find.byKey(const ValueKey('monthly-programs-card')), findsNothing);
  });

  testWidgets('a running one-week program shows its last day', (tester) async {
    await _pump(tester, () => _coach(
          unlocked: false,
          plan: {'planId': 'p2', 'limit': 'OneWeek', 'startDate': '2026-11-24', 'endsOn': '2026-11-30', 'hasEnded': false},
        ));

    expect(find.textContaining('برنامج أسبوع شغال'), findsOneWidget);
    expect(find.byKey(const ValueKey('unlock-week-c1')), findsOneWidget);
  });
}
