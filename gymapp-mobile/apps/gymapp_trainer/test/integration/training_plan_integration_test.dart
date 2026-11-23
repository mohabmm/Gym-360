import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_trainer/core/providers.dart';
import 'package:gymapp_trainer/core/router.dart';
import 'package:gymapp_trainer/features/plans/presentation/client_plan_tab.dart';
import 'package:gymapp_trainer/features/plans/presentation/plan_builder_screen.dart';

import 'helpers/test_api_client.dart';

late TestApiClient t;

Map<String, dynamic> _exercise(String key, String name) =>
    {'key': key, 'exerciseId': null, 'name': name, 'nameAr': null, 'sets': 3, 'reps': '10-12', 'restSeconds': 90};

Map<String, dynamic> _week(int w) => {
      'week': w,
      'days': [
        for (final d in ['A', 'B', 'C'])
          {'key': 'w$w-$d', 'title': 'Day $d – Full body', 'titleAr': null, 'exercises': [_exercise('w$w-$d-1', 'Goblet Squat')]}
      ],
    };

Map<String, dynamic> _summary({String id = 'tpl-gym', String scope = 'Gym', bool canEdit = false}) => {
      'id': id,
      'scope': scope,
      'name': 'Beginner full body – 3 days',
      'nameAr': null,
      'goal': 'GeneralFitness',
      'durationWeeks': 4,
      'daysPerWeek': 3,
      'isSample': scope == 'Gym',
      'canEdit': canEdit,
    };

Map<String, dynamic> _template() => {'summary': _summary(), 'document': {'schemaVersion': 1, 'weeks': [for (var w = 1; w <= 4; w++) _week(w)]}};

Map<String, dynamic> _plan({int version = 1}) => {
      'summary': {
        'id': 'plan-1',
        'traineeId': kClientId,
        'trainerId': 'coach-1',
        'sourceTemplateId': 'tpl-gym',
        'name': 'Beginner full body – 3 days',
        'goal': 'GeneralFitness',
        'durationWeeks': 4,
        'daysPerWeek': 3,
        'workoutCount': 12,
        'startDate': '2026-11-23',
        'status': 'Active',
        'version': version,
        'updatedAt': '2026-11-23T08:00:00Z',
      },
      'document': {'schemaVersion': 1, 'weeks': [for (var w = 1; w <= 4; w++) _week(w)]},
    };

Future<void> _pump(WidgetTester tester, {Map<String, dynamic>? active}) async {
  t.adapter.onGet(ApiEndpoints.traineePlans(kClientId), respond: (_) => t.adapter.ok({'active': active, 'history': const []}));
  t.adapter.onGet(ApiEndpoints.planTemplates,
      respond: (_) => t.adapter.ok([_summary(), _summary(id: 'tpl-mine', scope: 'Coach', canEdit: true)]));
  t.adapter.onGet(ApiEndpoints.planTemplateById('tpl-gym'), respond: (_) => t.adapter.ok(_template()));
  tester.view.physicalSize = const Size(1200, 2600);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.reset);
  final router = GoRouter(routes: [
    GoRoute(path: '/', builder: (_, __) => const Scaffold(body: ClientPlanTab(clientId: kClientId, isArabic: false))),
    GoRoute(path: AppRoutes.planBuilder, builder: (_, state) => PlanBuilderScreen(args: state.extra! as PlanBuilderArgs)),
  ]);
  await tester.pumpWidget(ProviderScope(
    overrides: [apiClientProvider.overrideWithValue(t.apiClient)],
    child: MaterialApp.router(
      routerConfig: router,
      locale: const Locale('en'),
      supportedLocales: const [Locale('ar'), Locale('en')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
    ),
  ));
  await tester.pumpAndSettle();
}

Map<String, dynamic> _sent(String method) => t.adapter.recorded.lastWhere((r) => r.method == method).bodyAsMap!;

void main() {
  setUp(() => t = buildTestApiClient());

  testWidgets('a gym template is given to a client in three taps, as a copy repeated for four weeks', (tester) async {
    t.adapter.onPost(ApiEndpoints.trainingPlans, respond: (_) => t.adapter.ok(_plan()));
    await _pump(tester);

    await tester.tap(find.text('Give a plan'));
    await tester.pumpAndSettle();
    expect(find.text('Start blank'), findsOneWidget, reason: 'building from scratch is one tap away too');
    expect(find.text('Gym templates'), findsOneWidget);
    await tester.tap(find.text('Beginner full body – 3 days').first);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Give plan · 12 workouts'));
    await tester.pumpAndSettle();

    final body = _sent('POST');
    expect(body['traineeId'], kClientId);
    expect(body['templateId'], 'tpl-gym');
    final content = body['content'] as Map;
    expect(content['repeatWeekOneForWeeks'], 4);
    expect((content['document'] as Map)['weeks'], hasLength(1));
    expect(((((content['document'] as Map)['weeks'] as List).first as Map)['days'] as List).first['key'], 'w1-A');
    expect(find.text('Plan given ✅ the client is notified'), findsOneWidget);
  });

  testWidgets('a plan built from scratch is not sent until it has a name and exercises', (tester) async {
    await _pump(tester);

    await tester.tap(find.text('Give a plan'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Start blank'));
    await tester.pumpAndSettle();
    await tester.tap(find.textContaining('Give plan ·'));
    await tester.pumpAndSettle();

    expect(find.text('Fill in the plan name and every day with its exercises'), findsOneWidget);
    expect(t.adapter.recorded.where((r) => r.method == 'POST'), isEmpty);
  });

  testWidgets('editing a plan sends the version it was loaded at and explains a conflict', (tester) async {
    t.adapter.onPut(ApiEndpoints.trainingPlanById('plan-1'),
        respond: (_) => t.adapter.error(409, code: 'Training.TrainingPlan.Conflict', message: 'changed'));
    await _pump(tester, active: _plan(version: 3));

    expect(find.text('Beginner full body – 3 days'), findsOneWidget);
    await tester.tap(find.text('Edit plan'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Save changes'));
    await tester.pumpAndSettle();

    expect(_sent('PUT')['expectedVersion'], 3);
    expect(find.text('The plan was changed elsewhere. Open it again.'), findsOneWidget);
  });
}
