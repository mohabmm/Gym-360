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

late TestApiClient t;

const _package = SessionPackageModel(
  id: 'pkg-1',
  memberId: kClientId,
  memberName: 'Mona Ali',
  totalSessions: 12,
  remainingSessions: 8,
);

Map<String, dynamic> _answer({bool already = false}) => {
      'sessionId': 'sess-1',
      'remainingSessions': already ? 8 : 7,
      'alreadyLogged': already,
      'date': '2026-11-11',
      'startTime': '18:00:00',
    };

Future<void> _pump(WidgetTester tester, {Map<String, dynamic>? answer, int status = 200}) async {
  t.adapter.onPost(ApiEndpoints.sessionQuickLog,
      respond: (_) => status == 200 ? t.adapter.ok(answer ?? _answer()) : t.adapter.rawResponse(status, answer));
  tester.view.physicalSize = const Size(1200, 2400);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.reset);
  final router = GoRouter(routes: [
    GoRoute(
      path: '/',
      builder: (_, __) => const Scaffold(body: Center(child: QuickLogButton(memberId: kClientId))),
    ),
    GoRoute(path: '/pt-sessions/:id/exercises', builder: (_, __) => const Scaffold(body: Text('exercises page'))),
    GoRoute(path: '/clients/:id/measurements', builder: (_, __) => const Scaffold(body: Text('measurements page'))),
  ]);
  await tester.pumpWidget(ProviderScope(
    overrides: [
      apiClientProvider.overrideWithValue(t.apiClient),
      allClientPackagesProvider.overrideWith((ref) async => [_package]),
      clientNameByIdProvider.overrideWithValue(const {}),
      isFeatureEnabledProvider.overrideWith((ref, flag) => true),
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
}

Map<String, dynamic> _sent() => t.adapter.recorded.firstWhere((r) => r.method == 'POST').bodyAsMap!;

void main() {
  setUp(() => t = buildTestApiClient());
  _theSheet();
  _afterRecording();
}

void _theSheet() {
  testWidgets('tapping Log session asks first: client, sessions left, when', (tester) async {
    await _pump(tester);

    expect(find.text('Mona Ali'), findsOneWidget);
    expect(find.text('8 of 12 left → 7 after this'), findsOneWidget);
    expect(find.text('When did it happen?'), findsOneWidget);
    final today = tester.widget<ChoiceChip>(find.byKey(const ValueKey('day-today')));
    expect(today.selected, isTrue);
    expect(t.adapter.recorded.where((r) => r.method == 'POST'), isEmpty, reason: 'nothing is deducted before confirming');
  });

  testWidgets('a forgotten session from yesterday is sent with its date, time and duration', (tester) async {
    await _pump(tester);

    await tester.tap(find.byKey(const ValueKey('day-yesterday')));
    await tester.pumpAndSettle();
    expect(find.textContaining('Yesterday ·'), findsOneWidget);
    await tester.tap(find.byKey(const ValueKey('record-session')));
    await tester.pumpAndSettle();

    final body = _sent();
    final yesterday = DateUtils.dateOnly(DateTime.now()).subtract(const Duration(days: 1));
    expect(body['memberId'], kClientId);
    expect(body['date'], '${yesterday.year}-${yesterday.month.toString().padLeft(2, '0')}-${yesterday.day.toString().padLeft(2, '0')}');
    expect(body['startTime'], matches(RegExp(r'^\d\d:\d\d:00$')));
    expect(body['durationMinutes'], 60);
  });

  testWidgets('server refusal keeps the sheet open with the reason', (tester) async {
    await _pump(tester, status: 409, answer: {
      'data': null,
      'errors': [
        {'code': 'Scheduling.QuickLog.TooOld.Conflict', 'message': 'too old'},
      ],
    });

    await tester.tap(find.byKey(const ValueKey('record-session')));
    await tester.pumpAndSettle();

    expect(find.byKey(const ValueKey('record-error')), findsOneWidget);
    expect(find.textContaining('up to 7 days later'), findsOneWidget);
    expect(find.byKey(const ValueKey('record-session')), findsOneWidget);
  });
}

void _afterRecording() {
  testWidgets('after recording: what was saved, sessions left, and next steps', (tester) async {
    await _pump(tester);

    await tester.tap(find.byKey(const ValueKey('record-session')));
    await tester.pumpAndSettle();

    expect(find.text('Session recorded ✅'), findsOneWidget);
    expect(find.textContaining('7 sessions left'), findsOneWidget);
    expect(find.textContaining('Mona Ali will be asked to confirm'), findsOneWidget);
    await tester.tap(find.byKey(const ValueKey('next-exercises')));
    await tester.pumpAndSettle();
    expect(find.text('exercises page'), findsOneWidget);
  });

  testWidgets('the same slot recorded again says so instead of failing silently', (tester) async {
    await _pump(tester, answer: _answer(already: true));

    await tester.tap(find.byKey(const ValueKey('record-session')));
    await tester.pumpAndSettle();

    expect(find.text('This session is already recorded'), findsOneWidget);
    expect(find.text('No extra session was used.'), findsOneWidget);
    await tester.tap(find.byKey(const ValueKey('next-measurements')));
    await tester.pumpAndSettle();
    expect(find.text('measurements page'), findsOneWidget);
  });
}
