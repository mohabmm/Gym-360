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

Map<String, dynamic> _programSummary(String key, String name, String category) => {
      'key': key,
      'name': name,
      'nameAr': name,
      'description': 'One muscle group a day',
      'descriptionAr': 'عضلة كل يوم',
      'category': category,
      'level': 'Intermediate',
      'goal': 'MuscleGain',
      'durationWeeks': 8,
      'daysPerWeek': 3,
    };

Map<String, dynamic> _program() => {
      'summary': _programSummary('classic_split_5d', 'Classic split', 'Egyptian'),
      'document': {'schemaVersion': 1, 'weeks': [for (var w = 1; w <= 8; w++) _week(w)]},
    };

Future<void> _pump(WidgetTester tester, {Map<String, dynamic>? active, bool nutrition = false, Map<String, dynamic>? access}) async {
  t.adapter.onGet(ApiEndpoints.traineePlans(kClientId), respond: (_) => t.adapter.ok({'active': active, 'history': const []}));
  t.adapter.onGet(ApiEndpoints.traineePlanAccess(kClientId),
      respond: (_) => t.adapter.ok(access ?? {'limit': 'None', 'maxDays': 0, 'limitedPlan': null}));
  t.adapter.onGet(ApiEndpoints.planTemplates,
      respond: (_) => t.adapter.ok([_summary(), _summary(id: 'tpl-mine', scope: 'Coach', canEdit: true)]));
  t.adapter.onGet(ApiEndpoints.planTemplateById('tpl-gym'), respond: (_) => t.adapter.ok(_template()));
  t.adapter.onGet(ApiEndpoints.planPrograms,
      respond: (_) => t.adapter.ok([_programSummary('ppl_3d', 'Push / pull / legs', 'Classic'), _programSummary('classic_split_5d', 'Classic split', 'Egyptian')]));
  t.adapter.onGet(ApiEndpoints.planProgramByKey('classic_split_5d'), respond: (_) => t.adapter.ok(_program()));
  tester.view.physicalSize = const Size(1200, 2600);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.reset);
  final router = GoRouter(routes: [
    GoRoute(path: '/', builder: (_, __) => const Scaffold(body: ClientPlanTab(clientId: kClientId, isArabic: false))),
    GoRoute(path: AppRoutes.planBuilder, builder: (_, state) => PlanBuilderScreen(args: state.extra! as PlanBuilderArgs)),
  ]);
  await tester.pumpWidget(ProviderScope(
    overrides: [
      apiClientProvider.overrideWithValue(t.apiClient),
      isFeatureEnabledProvider.overrideWith((ref, key) => nutrition && key == FeatureFlagKeys.nutrition),
    ],
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

  testWidgets('a plan built from scratch saves with a free-text exercise and no day name typed', (tester) async {
    t.adapter.onGet(ApiEndpoints.planExercises, respond: (_) => t.adapter.ok({'items': const []}));
    t.adapter.onPost(ApiEndpoints.trainingPlans, respond: (_) => t.adapter.ok(_plan()));
    await _pump(tester);

    await tester.tap(find.text('Give a plan'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Start blank'));
    await tester.pumpAndSettle();
    await tester.enterText(find.widgetWithText(TextFormField, 'Plan name'), 'My client plan');
    await tester.tap(find.text('Add exercise'));
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextField).last, 'Squat');
    await tester.pumpAndSettle();
    await tester.tap(find.text('Use "Squat" as a free-text exercise'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Done'));
    await tester.pumpAndSettle();
    await tester.tap(find.textContaining('Give plan ·'));
    await tester.pumpAndSettle();

    final content = _sent('POST')['content'] as Map;
    final day = ((((content['document'] as Map)['weeks'] as List).first as Map)['days'] as List).first as Map;
    expect(content['name'], 'My client plan');
    expect(day['title'], 'Day 1');
    expect(((day['exercises'] as List).first as Map)['name'], 'Squat');
    expect(find.text('Plan given ✅ the client is notified'), findsOneWidget);
  });

  testWidgets('a ready program is filtered by category and given to a client as a copy, not a template', (tester) async {
    t.adapter.onPost(ApiEndpoints.trainingPlans, respond: (_) => t.adapter.ok(_plan()));
    await _pump(tester);

    await tester.tap(find.text('Give a plan'));
    await tester.pumpAndSettle();
    expect(find.text('Ready programs'), findsOneWidget);
    await tester.tap(find.text('Egyptian gyms'));
    await tester.pumpAndSettle();
    expect(find.text('Push / pull / legs'), findsNothing, reason: 'the filter hides other groups');
    await tester.tap(find.text('Classic split'));
    await tester.pumpAndSettle();
    await tester.tap(find.textContaining('Give plan ·'));
    await tester.pumpAndSettle();

    final body = _sent('POST');
    expect(body['templateId'], isNull);
    final content = body['content'] as Map;
    expect(content['name'], 'Classic split');
    expect(content['goal'], 'MuscleGain');
    expect(content['repeatWeekOneForWeeks'], 8);
  });

    testWidgets('onboarding: after the plan is given the coach is offered the diet (nutrition on only)', (tester) async {
    t.adapter.onPost(ApiEndpoints.trainingPlans, respond: (_) => t.adapter.ok(_plan()));
    await _pump(tester, nutrition: true);

    await tester.tap(find.text('Give a plan'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Beginner full body – 3 days').first);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Give plan · 12 workouts'));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('diet-onboarding-offer')), findsOneWidget);
    expect(find.text("Complete the client's onboarding"), findsOneWidget);
    await tester.tap(find.text('Later'));
    await tester.pumpAndSettle();
    expect(find.byKey(const Key('diet-onboarding-offer')), findsNothing);
  });

  testWidgets('with nutrition off no diet is offered after the plan', (tester) async {
    t.adapter.onPost(ApiEndpoints.trainingPlans, respond: (_) => t.adapter.ok(_plan()));
    await _pump(tester);

    await tester.tap(find.text('Give a plan'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Beginner full body – 3 days').first);
    await tester.pumpAndSettle();
    await tester.tap(find.text('Give plan · 12 workouts'));
    await tester.pumpAndSettle();

    expect(find.byKey(const Key('diet-onboarding-offer')), findsNothing);
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

  group('pt.starter_plan', () {
    const locked = {'limit': 'Starter', 'maxDays': 3, 'limitedPlan': null};
    const week = {'limit': 'OneWeek', 'maxDays': 7, 'limitedPlan': null};
    const month = {'limit': 'ThirtyDays', 'lengthDays': 30, 'maxWeeks': 4, 'limitedPlan': null};

    testWidgets('while templates are locked the coach gives a 3-day plan from a ready program\'s first days', (tester) async {
      t.adapter.onPost(ApiEndpoints.trainingPlans, respond: (_) => t.adapter.ok(_plan()));
      await _pump(tester, access: locked);

      await tester.tap(find.text('3-day starter plan'));
      await tester.pumpAndSettle();
      expect(find.text('Build a 3-day plan'), findsOneWidget);
      expect(find.byKey(const ValueKey('programs-locked')), findsOneWidget);
      expect(find.text('Gym templates'), findsNothing, reason: 'templates stay closed until the desk unlocks them');
      await tester.tap(find.text('Classic split'));
      await tester.pumpAndSettle();
      expect(find.byKey(const ValueKey('plan-limits')), findsOneWidget);
      expect(find.text('Duration'), findsNothing, reason: 'a starter plan is one week');
      await tester.tap(find.textContaining('Give plan ·'));
      await tester.pumpAndSettle();

      final body = _sent('POST');
      expect(body['templateId'], isNull);
      final content = body['content'] as Map;
      expect(content['repeatWeekOneForWeeks'], 1);
      final weeks = (content['document'] as Map)['weeks'] as List;
      expect(weeks, hasLength(1));
      expect((weeks.first as Map)['days'], hasLength(3));
    });

    testWidgets('a starter plan that is over says the desk can unlock a one-week program', (tester) async {
      final plan = _plan();
      (plan['summary'] as Map)
        ..['limit'] = 'Starter'
        ..['endsOn'] = '2026-11-25'
        ..['hasEnded'] = true;
      await _pump(tester, active: plan, access: locked);

      expect(find.byKey(const ValueKey('limit-status')), findsOneWidget);
      expect(find.text('The starter plan is over. If the member stops coming, ask the desk to unlock a one-week or 30-day program.'),
          findsOneWidget);
    });

    testWidgets('after the desk unlocks a week, a gym template is given as its first week, then the diet is offered', (tester) async {
      t.adapter.onPost(ApiEndpoints.trainingPlans, respond: (_) => t.adapter.ok(_plan()));
      await _pump(tester, access: week, nutrition: true);

      await tester.tap(find.text('One-week program'));
      await tester.pumpAndSettle();
      expect(find.text('Gym templates'), findsOneWidget, reason: 'templates are open for the week');
      await tester.tap(find.text('Beginner full body – 3 days').first);
      await tester.pumpAndSettle();
      expect(find.byKey(const ValueKey('plan-limits')), findsOneWidget);
      await tester.tap(find.textContaining('Give plan ·'));
      await tester.pumpAndSettle();

      final body = _sent('POST');
      expect(body['templateId'], 'tpl-gym');
      final content = body['content'] as Map;
      expect(content['repeatWeekOneForWeeks'], 1);
      expect((content['document'] as Map)['weeks'], hasLength(1));
      expect(find.byKey(const Key('diet-onboarding-offer')), findsOneWidget, reason: 'plan and diet together');
    });

    testWidgets('the server refusing a longer plan is explained', (tester) async {
      t.adapter.onPost(ApiEndpoints.trainingPlans,
          respond: (_) => t.adapter.error(403, code: 'Training.StarterPlan.Forbidden', message: 'starter only'));
      await _pump(tester);

      await tester.tap(find.text('Give a plan'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Beginner full body – 3 days').first);
      await tester.pumpAndSettle();
      await tester.tap(find.text('Give plan · 12 workouts'));
      await tester.pumpAndSettle();

      expect(find.text('Templates are still locked: give a 3-day plan only.'), findsOneWidget);
    });

    testWidgets('after the desk unlocks 30 days, a 4-week gym template is given whole and the weeks stay capped', (tester) async {
      t.adapter.onPost(ApiEndpoints.trainingPlans, respond: (_) => t.adapter.ok(_plan()));
      await _pump(tester, access: month);

      await tester.tap(find.text('30-day program'));
      await tester.pumpAndSettle();
      expect(find.text('Gym templates'), findsOneWidget);
      await tester.tap(find.text('Beginner full body – 3 days').first);
      await tester.pumpAndSettle();
      expect(find.text('30-day program: up to 4 weeks, ends after 30 days.'), findsOneWidget);
      expect(find.text('Duration'), findsOneWidget, reason: 'weeks can be chosen, up to 4');
      await tester.tap(find.textContaining('Give plan ·'));
      await tester.pumpAndSettle();

      final body = _sent('POST');
      expect(body['templateId'], 'tpl-gym');
      expect((body['content'] as Map)['repeatWeekOneForWeeks'], 4);
    });
  });
}
