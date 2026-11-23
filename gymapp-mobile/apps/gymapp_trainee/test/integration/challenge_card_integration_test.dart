import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/core/providers.dart';
import 'package:gymapp_trainee/features/plan/data/challenge_providers.dart';
import 'package:gymapp_trainee/features/plan/data/plan_providers.dart';
import 'package:gymapp_trainee/features/plan/data/plan_workout_queue.dart';
import 'package:gymapp_trainee/features/plan/presentation/challenge_card.dart';

import 'helpers/sample_payloads.dart';
import 'helpers/test_api_client.dart';

class _SignedIn extends AuthNotifier {
  @override
  Future<AuthState> build() async =>
      AuthState(status: AuthStatus.authenticated, user: UserModel.fromJson(Samples.user()), currentGymId: 'gym-1');
}

Map<String, dynamic> _challenge({String status = 'active', int count = 8, String? deliveredAt}) => {
      'status': status,
      'count': count,
      'target': 12,
      'windowStart': '2026-11-01',
      'windowEnd': '2026-11-30',
      'daysLeft': 9,
      'rewardTextAr': 'جلسة متابعة مع المدرب: قياسات جديدة وجدول الشهر الجاي',
      'rewardTextEn': "A follow-up session with your coach: new measurements and next month's plan",
      'reachedAt': status == 'reached' ? '2026-11-20T10:00:00Z' : null,
      'deliveredAt': deliveredAt,
    };

class _Harness {
  final TestApiClient api = buildTestApiClient();
  final MemoryPlanWorkoutQueue queue = MemoryPlanWorkoutQueue();
  final MemoryChallengeCache cache = MemoryChallengeCache();
  Object Function() respond;

  _Harness(Map<String, dynamic> json) : respond = (() => json) {
    api.adapter.onGet(ApiEndpoints.myChallenge, respond: (_) {
      final r = respond();
      if (r is Exception) throw r;
      return r is int ? api.adapter.error(403, code: 'FEATURE_DISABLED:mem.first_month_challenge') : api.adapter.ok(r);
    });
  }

  Future<void> pump(WidgetTester tester, {String locale = 'ar'}) async {
    await tester.pumpWidget(ProviderScope(
      overrides: [
        apiClientProvider.overrideWithValue(api.apiClient),
        authProvider.overrideWith(_SignedIn.new),
        planWorkoutQueueProvider.overrideWithValue(queue),
        challengeCacheProvider.overrideWithValue(cache),
      ],
      child: MaterialApp(
        locale: Locale(locale),
        supportedLocales: const [Locale('ar'), Locale('en')],
        localizationsDelegates: GlobalMaterialLocalizations.delegates,
        home: const Scaffold(body: ChallengeCard()),
      ),
    ));
    for (var i = 0; i < 5; i++) {
      await tester.pump(const Duration(milliseconds: 50));
    }
    await tester.pumpAndSettle();
  }
}

void main() {
  testWidgets('Arabic card shows 8 / 12, what is left and the reward', (tester) async {
    await _Harness(_challenge()).pump(tester);

    expect(find.text('تحدي الشهر الأول'), findsOneWidget);
    expect(find.text('8 / 12 تمرين'), findsOneWidget);
    expect(find.text('فاضل 4 تمارين · 9 يوم باقيين'), findsOneWidget);
    expect(find.textContaining('جلسة متابعة مع المدرب'), findsOneWidget);
  });

  testWidgets('a workout saved offline counts on the card straight away', (tester) async {
    final h = _Harness(_challenge());
    await h.queue.add(PendingPlanWorkout(
      clientId: 'c1', userId: Samples.user()['id'] as String, planId: 'p1', dayKey: 'd-a',
      finishedAt: DateTime(2026, 11, 21), body: const {},
    ));
    await h.pump(tester, locale: 'en');

    expect(find.text('9 / 12 workouts'), findsOneWidget);
  });

  testWidgets('offline it shows the last copy from the phone', (tester) async {
    final h = _Harness(_challenge(count: 5));
    await h.cache.write(Samples.user()['id'] as String, _challenge(count: 5));
    h.respond = () => const SocketException('offline');
    await h.pump(tester);

    expect(find.text('5 / 12 تمرين'), findsOneWidget);
  });

  testWidgets('won: congratulations and how to claim, until the desk hands it over', (tester) async {
    await _Harness(_challenge(status: 'reached', count: 12)).pump(tester);
    expect(find.text('مبروك! كسبت التحدي 🎉'), findsOneWidget);
    expect(find.textContaining('كلّم الاستقبال'), findsOneWidget);
  });

  for (final (name, json) in [
    ('delivered', _challenge(status: 'reached', count: 12, deliveredAt: '2026-11-22T10:00:00Z')),
    ('window over', _challenge(status: 'ended', count: 3)),
    ('not started', _challenge(status: 'not_started', count: 0)),
  ]) {
    testWidgets('no card when $name', (tester) async {
      await _Harness(json).pump(tester);
      expect(find.byKey(const Key('challenge-card')), findsNothing);
    });
  }

  testWidgets('no card when the gym has the challenge off', (tester) async {
    final h = _Harness(_challenge());
    h.respond = () => 403;
    await h.pump(tester);
    expect(find.byKey(const Key('challenge-card')), findsNothing);
  });
}
