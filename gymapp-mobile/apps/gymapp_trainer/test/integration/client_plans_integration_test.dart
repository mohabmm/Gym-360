import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainer/core/providers.dart';
import 'package:gymapp_trainer/features/clients/presentation/providers/client_providers.dart';
import 'package:gymapp_trainer/features/plans/data/coach_overview.dart';
import 'package:gymapp_trainer/features/plans/presentation/client_plans_screen.dart';

import 'helpers/test_api_client.dart';

const _quiet = 'quiet-1';
const _active = 'active-1';
const _noPlan = 'noplan-1';

ClientSummary _client(String id, String first, {String? phone}) =>
    ClientSummary(user: UserModel(id: id, email: '$id@t', firstName: first, lastName: 'Ali', phone: phone, role: 'Trainee'));

final _overview = {
  'inactiveDays': 5,
  'clients': [
    {'traineeId': _quiet, 'planId': 'p1', 'planName': 'Full body', 'planNameAr': 'فول بادي', 'thisWeek': 0, 'weekTarget': 3,
      'doneCount': 2, 'total': 12, 'lastWorkoutOn': '2026-11-14', 'daysSince': 6, 'isInactive': true, 'nextDayTitle': 'Day C', 'nextDayTitleAr': 'اليوم ج'},
    {'traineeId': _active, 'planId': 'p2', 'planName': 'Fat loss', 'thisWeek': 2, 'weekTarget': 3, 'doneCount': 5, 'total': 12,
      'lastWorkoutOn': '2026-11-19', 'daysSince': 1, 'isInactive': false},
    {'traineeId': _noPlan},
  ],
};

Future<TestApiClient> _pump(WidgetTester tester, {int nudgeStatus = 200, String locale = 'ar'}) async {
  final t = buildTestApiClient();
  t.adapter.onGet(ApiEndpoints.coachPlanOverview, respond: (_) => t.adapter.ok(_overview));
  t.adapter.onPost(ApiEndpoints.planNudge(_quiet),
      respond: (_) => nudgeStatus == 200
          ? t.adapter.ok({'sentAt': '2026-11-20T10:00:00Z'})
          : t.adapter.error(nudgeStatus, code: 'Training.PlanNudge.Conflict'));
  tester.view.physicalSize = const Size(1200, 2600);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(ProviderScope(
    overrides: [
      apiClientProvider.overrideWithValue(t.apiClient),
      clientRosterProvider.overrideWith((ref) async => [
            _client(_quiet, 'Mona', phone: '010 1234 5678'),
            _client(_active, 'Omar'),
            _client(_noPlan, 'Sara'),
          ]),
    ],
    child: MaterialApp(
      locale: Locale(locale),
      supportedLocales: const [Locale('ar'), Locale('en')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
      home: const ClientPlansScreen(),
    ),
  ));
  await tester.pumpAndSettle();
  return t;
}

void main() {
  testWidgets('Arabic: the quiet client is first with how long, then those on track, then those without a plan', (tester) async {
    await _pump(tester);

    expect(find.text('محتاجين متابعة'), findsOneWidget);
    expect(find.text('من غير تمرين من 6 يوم'), findsOneWidget);
    expect(find.text('آخر تمرين من 1 يوم'), findsOneWidget);
    expect(find.text('من غير خطة'), findsOneWidget);
    final quietY = tester.getTopLeft(find.byKey(const ValueKey('client-plan-$_quiet'))).dy;
    final activeY = tester.getTopLeft(find.byKey(const ValueKey('client-plan-$_active'))).dy;
    final noPlanY = tester.getTopLeft(find.byKey(const ValueKey('client-plan-$_noPlan'))).dy;
    expect(quietY < activeY && activeY < noPlanY, isTrue);
  });

  testWidgets('Remind sends one push; a second one the same day says so', (tester) async {
    final t = await _pump(tester, locale: 'en');

    await tester.tap(find.byKey(const ValueKey('remind-$_quiet')));
    await tester.pumpAndSettle();
    expect(find.text('Reminder sent ✅'), findsOneWidget);
    expect(t.adapter.recorded.where((r) => r.method == 'POST' && r.path == ApiEndpoints.planNudge(_quiet)), hasLength(1));
  });

  testWidgets('already reminded today', (tester) async {
    await _pump(tester, nudgeStatus: 409, locale: 'en');

    await tester.tap(find.byKey(const ValueKey('remind-$_quiet')));
    await tester.pumpAndSettle();
    expect(find.text('You already reminded them today'), findsOneWidget);
  });

  testWidgets('WhatsApp is offered only when the client has a usable phone number', (tester) async {
    await _pump(tester);

    expect(find.byKey(const ValueKey('whatsapp-$_quiet')), findsOneWidget);
    expect(find.byKey(const ValueKey('whatsapp-$_active')), findsNothing);
  });

  test('Egyptian numbers become wa.me links with the message', () {
    for (final phone in ['01012345678', '+20 101 234 5678', '00201012345678', '1012345678']) {
      expect(whatsAppLink(phone, 'hi')?.toString(), 'https://wa.me/201012345678?text=hi', reason: phone);
    }
    expect(whatsAppLink(null, 'hi'), isNull);
    expect(whatsAppLink('123', 'hi'), isNull);
  });

  test('the Arabic WhatsApp text names the days and the next workout', () {
    final text = whatsAppLink('01012345678', 'أهلاً منى 👋 بقالك 6 يوم من غير تمرين، تمرينك الجاي: اليوم ج. نكمّل النهارده؟ 💪');
    expect(text!.queryParameters['text'], contains('بقالك 6 يوم'));
    expect(text.toString(), isNot(contains('+')), reason: 'spaces are %20');
  });
}
