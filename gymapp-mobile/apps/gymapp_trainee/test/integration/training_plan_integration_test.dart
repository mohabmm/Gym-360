import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainee/core/providers.dart';
import 'package:gymapp_trainee/features/plan/data/plan_providers.dart';
import 'package:gymapp_trainee/features/plan/data/plan_workout_queue.dart';
import 'package:gymapp_trainee/features/plan/presentation/my_plan_screen.dart';
import 'package:gymapp_trainee/features/plan/presentation/plan_routes.dart';
import 'package:gymapp_trainee/features/plan/presentation/plan_today_card.dart';
import 'package:gymapp_trainee/features/plan/presentation/plan_workout_screen.dart';
import 'package:intl/date_symbol_data_local.dart';

import '../features/plan/plan_fixtures.dart';
import 'helpers/sample_payloads.dart';
import 'helpers/mock_http_adapter.dart';
import 'helpers/test_api_client.dart';

class _PtFlags implements FeatureFlagService {
  @override
  FeatureFlags get current => FeatureFlags.ptDefaults();
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

class _SignedIn extends AuthNotifier {
  @override
  Future<AuthState> build() async =>
      AuthState(status: AuthStatus.authenticated, user: UserModel.fromJson(Samples.user()), currentGymId: 'gym-1');
}

final _put = RegExp(r'^/api/v1/training/me/plan-workouts/[0-9a-f-]{36}$');

class _Harness {
  final TestApiClient api = buildTestApiClient();
  final MemoryPlanWorkoutQueue queue = MemoryPlanWorkoutQueue();
  bool online = true;

  _Harness() {
    api.adapter.onGet(ApiEndpoints.myPlan, respond: (_) {
      if (!online) throw const SocketException('offline');
      return api.adapter.ok(myPlanJson());
    });
    api.adapter.onPutMatching(_put, respond: (request) {
      if (!online) throw const SocketException('offline');
      return api.adapter.ok({'outcome': 'created', 'workout': {'id': 'w1', 'clientId': request.path.split('/').last}});
    });
  }

  List<RecordedRequest> get uploads => api.adapter.recorded.where((r) => r.method == 'PUT').toList();

  Future<void> pump(WidgetTester tester, {String locale = 'ar'}) async {
    tester.view.physicalSize = const Size(1080, 4000);
    tester.view.devicePixelRatio = 2.5;
    addTearDown(tester.view.reset);
    final router = GoRouter(routes: [
      GoRoute(path: '/', builder: (_, __) => const Scaffold(body: SingleChildScrollView(child: PlanTodayCard()))),
      GoRoute(
        path: PlanRoutes.plan,
        builder: (_, __) => const MyPlanScreen(),
        routes: [GoRoute(path: 'workout/:dayKey', builder: (_, s) => PlanWorkoutScreen(dayKey: s.pathParameters['dayKey']!))],
      ),
    ]);
    await tester.pumpWidget(ProviderScope(
      overrides: [
        apiClientProvider.overrideWithValue(api.apiClient),
        authProvider.overrideWith(_SignedIn.new),
        featureFlagServiceProvider.overrideWithValue(_PtFlags()),
        planWorkoutQueueProvider.overrideWithValue(queue),
        myPlanCacheProvider.overrideWithValue(MemoryMyPlanCache()),
      ],
      child: MaterialApp.router(
        routerConfig: router,
        locale: Locale(locale),
        supportedLocales: const [Locale('ar'), Locale('en')],
        localizationsDelegates: GlobalMaterialLocalizations.delegates,
      ),
    ));
    await tester.pumpAndSettle();
  }

  /// Lets the mocked network answer: an HTTP call alone schedules no frame.
  Future<void> settle(WidgetTester tester) async {
    for (var i = 0; i < 5; i++) {
      await tester.pump(const Duration(milliseconds: 50));
    }
    await tester.pumpAndSettle();
  }

  Future<void> doFirstSetAndFinish(WidgetTester tester) async {
    await tester.tap(find.byKey(const Key('plan-start-workout')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('set-tick-e-squat-1-1')));
    await tester.pump();
    await tester.tap(find.byKey(const Key('plan-finish-workout')));
    await tester.pumpAndSettle();
  }
}

void main() {
  setUpAll(() async {
    await initializeDateFormatting('ar');
    await initializeDateFormatting('en');
  });

  testWidgets('Arabic home card shows week 1 day A; finishing it uploads once with the set and the last weight', (tester) async {
    final h = _Harness();
    await h.pump(tester);

    expect(find.text('تمرين النهارده'), findsOneWidget);
    expect(find.text('الأسبوع 1 · اليوم أ'), findsOneWidget);
    expect(find.text('الأسبوع ده: 0 من 2'), findsOneWidget);

    await h.doFirstSetAndFinish(tester);

    expect(find.text('تمام! التمرين اتسجل 💪'), findsOneWidget);
    final upload = h.uploads.single;
    expect(upload.body['dayKey'], 'w1a');
    expect(upload.body['planId'], 'plan-1');
    final squat = (upload.body['sets'] as List).cast<Map>().firstWhere((s) => s['exerciseKey'] == 'e-squat-1');
    expect(squat['done'], isTrue);
    expect(squat['weightKg'], 40, reason: 'prefilled from last time');
    expect(squat['reps'], 10);
    expect(h.queue.all(), isEmpty);
  });

  testWidgets('in a basement gym with no signal the workout is kept on the phone, then uploads once when back online', (tester) async {
    final h = _Harness();
    await h.pump(tester);
    h.online = false;

    await h.doFirstSetAndFinish(tester);

    expect(find.text('التمرين اتحفظ على الموبايل وهيتبعت أول ما النت يرجع'), findsOneWidget);
    expect(h.queue.all(), hasLength(1));
    // The card falls back to the cached plan and already moves on to day B.
    expect(find.text('خلّصت تمرين النهارده 💪'), findsOneWidget);
    expect(find.text('المرة الجاية: الأسبوع 1 · اليوم ب'), findsOneWidget);
    expect(find.textContaining('محفوظ على الموبايل'), findsOneWidget);

    // Back online: the home screen refreshes the plan (pull to refresh / next open).
    h.online = true;
    final container = ProviderScope.containerOf(tester.element(find.byType(PlanTodayCard)));
    container.invalidate(myPlanProvider);
    await h.settle(tester);
    expect(h.queue.all(), isEmpty);
    final clientIds = h.uploads.map((r) => r.path).toSet();
    expect(clientIds, hasLength(1), reason: 'every attempt reused the same client id');
  });

  testWidgets('leaving with ticked sets asks first', (tester) async {
    final h = _Harness();
    await h.pump(tester, locale: 'en');
    await tester.tap(find.byKey(const Key('plan-start-workout')));
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const Key('set-tick-e-squat-1-1')));
    await tester.pump();

    await tester.binding.handlePopRoute();
    await tester.pumpAndSettle();

    expect(find.text('Leave the workout?'), findsOneWidget);
    await tester.tap(find.text('Stay'));
    await tester.pumpAndSettle();
    expect(find.text('Finish workout (1/3)'), findsOneWidget);
  });
}
