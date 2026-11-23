import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainer/core/providers.dart';
import 'package:gymapp_trainer/features/clients/presentation/providers/client_providers.dart';
import 'package:gymapp_trainer/features/revenue/presentation/providers/revenue_providers.dart';
import 'package:gymapp_trainer/features/sessions/presentation/pt/pt_coach_widgets.dart';

import 'helpers/test_api_client.dart';

const _package = SessionPackageModel(id: 'pkg-1', memberId: kClientId, memberName: 'Mona Ali', totalSessions: 12, remainingSessions: 8);

Map<String, dynamic> _day(String key, String title) => {
      'key': key,
      'title': title,
      'exercises': [
        {'key': 'e-$key', 'name': 'Squat', 'sets': 3, 'reps': '10'},
      ],
    };

final _plans = {
  'active': {
    'summary': {'id': 'plan-1', 'name': 'Full body', 'startDate': '2026-11-14', 'updatedAt': '2026-11-14T00:00:00Z'},
    'document': {
      'weeks': [
        {'week': 1, 'days': [_day('w1a', 'Day A'), _day('w1b', 'Day B')]},
        {'week': 2, 'days': [_day('w2a', 'Day A'), _day('w2b', 'Day B')]},
      ],
    },
  },
  'history': [],
};

/// Opens "Record session" for a client with (or without) a plan; the trainee's next day is week 2 day A.
Future<TestApiClient> _open(WidgetTester tester, {bool withPlan = true, bool plansOn = true}) async {
  final t = buildTestApiClient();
  t.adapter.onPost(ApiEndpoints.sessionQuickLog,
      respond: (_) => t.adapter.ok({'sessionId': 's-1', 'remainingSessions': 7, 'alreadyLogged': false, 'date': '2026-11-20', 'startTime': '17:30:00'}));
  t.adapter.onGet(ApiEndpoints.traineePlans(kClientId),
      respond: (_) => t.adapter.ok(withPlan ? _plans : {'active': null, 'history': []}));
  t.adapter.onGet(ApiEndpoints.traineePlanProgress(kClientId),
      respond: (_) => t.adapter.ok({'cursor': {'nextDayKey': 'w2a', 'lastDoneIndex': 1, 'total': 4}, 'stats': {}, 'history': [], 'exercises': []}));
  tester.view.physicalSize = const Size(1200, 2400);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.reset);
  final router = GoRouter(routes: [
    GoRoute(path: '/', builder: (_, __) => const Scaffold(body: Center(child: QuickLogButton(memberId: kClientId)))),
  ]);
  await tester.pumpWidget(ProviderScope(
    overrides: [
      apiClientProvider.overrideWithValue(t.apiClient),
      clockProvider.overrideWithValue(() => DateTime(2026, 11, 20, 18, 30)),
      allClientPackagesProvider.overrideWith((ref) async => [_package]),
      clientNameByIdProvider.overrideWithValue(const {}),
      isFeatureEnabledProvider.overrideWith((ref, flag) => plansOn || flag != FeatureFlagKeys.ptTrainingPlan),
    ],
    child: MaterialApp.router(
      routerConfig: router,
      locale: const Locale('en'),
      supportedLocales: const [Locale('ar'), Locale('en')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
    ),
  ));
  await tester.pumpAndSettle();
  await tester.tap(find.byKey(const ValueKey('log-session')));
  await tester.pumpAndSettle();
  return t;
}

Map<String, dynamic> _sent(TestApiClient t) => t.adapter.recorded.firstWhere((r) => r.method == 'POST').bodyAsMap!;

Future<void> _record(WidgetTester tester) async {
  await tester.tap(find.byKey(const ValueKey('record-session')));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('the trainee\'s next plan day is pre-selected and sent with the session', (tester) async {
    final t = await _open(tester);

    expect(find.text('Plan day (optional)'), findsOneWidget);
    expect(find.text('Day A · Next'), findsOneWidget);
    expect(tester.widget<ChoiceChip>(find.byKey(const ValueKey('plan-day-w2a'))).selected, isTrue);
    await _record(tester);

    expect(_sent(t)['planDay'], {'planId': 'plan-1', 'dayKey': 'w2a'});
    expect(_sent(t)['memberId'], kClientId, reason: 'the session itself is the same');
  });

  testWidgets('the coach can pick another day of the week or none', (tester) async {
    final t = await _open(tester);

    await tester.tap(find.byKey(const ValueKey('plan-day-none')));
    await tester.pumpAndSettle();
    await _record(tester);

    expect(_sent(t).containsKey('planDay'), isFalse);
  });

  testWidgets('another day of the shown week can be chosen', (tester) async {
    final t = await _open(tester);

    await tester.tap(find.byKey(const ValueKey('plan-day-w2b')));
    await tester.pumpAndSettle();
    await _record(tester);

    expect((_sent(t)['planDay'] as Map)['dayKey'], 'w2b');
  });

  testWidgets('no plan, or plans off: the sheet is exactly as before', (tester) async {
    for (final (withPlan, plansOn) in [(false, true), (true, false)]) {
      final t = await _open(tester, withPlan: withPlan, plansOn: plansOn);

      expect(find.text('Plan day (optional)'), findsNothing);
      await _record(tester);
      expect(_sent(t).containsKey('planDay'), isFalse);
      await tester.pumpWidget(const SizedBox());
    }
  });
}
