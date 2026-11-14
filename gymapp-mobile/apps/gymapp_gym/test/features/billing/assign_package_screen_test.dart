import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_gym/core/providers.dart';
import 'package:gymapp_gym/features/billing/presentation/providers/billing_provider.dart';
import 'package:gymapp_gym/features/billing/presentation/screens/assign_package_screen.dart';
import 'package:gymapp_gym/features/members/presentation/providers/members_provider.dart';
import 'package:gymapp_gym/features/members/presentation/widgets/current_coach.dart';
import 'package:gymapp_gym/features/trainers/presentation/providers/trainers_provider.dart';

import '../../integration/helpers/test_api_client.dart';

const _mona = UserModel(
  id: 'member-1',
  email: 'mona@gym.eg',
  firstName: 'Mona',
  lastName: 'Ali',
  phone: '01001234567',
  memberNumber: '0012',
  role: 'trainee',
);
const _omar = UserModel(id: 'coach-1', email: 'o@gym.eg', firstName: 'Omar', lastName: 'Coach', role: 'trainer');
const _template = SessionPackageTemplateModel(
  id: 'tpl-12',
  gymId: 'gym-1',
  name: '12 sessions',
  sessionCount: 12,
  totalPrice: 1800,
  currency: 'EGP',
);

/// What the desk sees for one test: current coach (if any), and where the screen was opened from.
class _Desk {
  final SessionPackageModel? currentCoach;
  final String? fromMemberProfile;

  const _Desk({this.currentCoach, this.fromMemberProfile});
}

Future<TestApiClient> _pump(WidgetTester tester, [_Desk desk = const _Desk()]) async {
  final t = buildTestApiClient();
  t.adapter.onPost(ApiEndpoints.assignPackage, respond: (_) => t.adapter.created('pkg-1'));
  tester.view.physicalSize = const Size(1200, 3000);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(ProviderScope(
    overrides: [
      apiClientProvider.overrideWithValue(t.apiClient),
      currentGymIdProvider.overrideWithValue('gym-1'),
      memberSearchProvider.overrideWith((ref, q) async => q.contains('mon') || q.contains('12') ? [_mona] : []),
      memberDetailProvider.overrideWith((ref, id) async => _mona),
      memberCurrentCoachProvider.overrideWith((ref, id) async => desk.currentCoach),
      trainersProvider.overrideWith((ref) async => [_omar]),
      packageTemplatesProvider.overrideWith((ref) async => [_template]),
    ],
    child: MaterialApp(
      locale: const Locale('en'),
      supportedLocales: const [Locale('ar'), Locale('en')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
      home: Builder(
        builder: (context) => Scaffold(
          body: TextButton(
            onPressed: () => Navigator.of(context).push(MaterialPageRoute<void>(
              builder: (_) => AssignPackageScreen(initialMemberId: desk.fromMemberProfile),
            )),
            child: const Text('open'),
          ),
        ),
      ),
    ),
  ));
  await tester.tap(find.text('open'));
  await tester.pumpAndSettle();
  return t;
}

Future<void> _search(WidgetTester tester, String text) async {
  await tester.enterText(find.byKey(const ValueKey('member-search')), text);
  await tester.pump(const Duration(milliseconds: 350));
  await tester.pumpAndSettle();
}

bool _canAssign(WidgetTester tester) {
  final button = find.descendant(
    of: find.byKey(const ValueKey('assign-package')),
    matching: find.byWidgetPredicate((w) => w is ButtonStyleButton),
  );
  return tester.widget<ButtonStyleButton>(button).onPressed != null;
}

void main() {
  _findingTheMember();
  _coachAndPackage();
}

void _findingTheMember() {
  testWidgets('staff search by name instead of typing a member id', (tester) async {
    await _pump(tester);

    expect(find.text('Search by name, phone or member number'), findsOneWidget);
    expect(find.text('Pick the member first'), findsOneWidget);
    await _search(tester, 'mon');

    expect(find.text('Mona Ali'), findsOneWidget);
    expect(find.text('#0012 · 01001234567'), findsOneWidget);
    await tester.tap(find.text('Mona Ali'));
    await tester.pumpAndSettle();
    expect(find.byKey(const ValueKey('selected-member')), findsOneWidget);
    expect(find.byKey(const ValueKey('trainer-dropdown')), findsOneWidget);
  });

  testWidgets('no match says so', (tester) async {
    await _pump(tester);
    await _search(tester, 'zz');
    expect(find.text('No member matches this search'), findsOneWidget);
  });

  testWidgets('opened from a member profile: the member is already chosen and can be changed', (tester) async {
    await _pump(tester, const _Desk(fromMemberProfile: 'member-1'));

    expect(find.byKey(const ValueKey('selected-member')), findsOneWidget);
    await tester.tap(find.text('Change'));
    await tester.pumpAndSettle();
    expect(find.byKey(const ValueKey('member-search')), findsOneWidget);
  });
}

void _coachAndPackage() {
  testWidgets('member with a coach: renewal is locked to that coach', (tester) async {
    await _pump(
      tester,
      const _Desk(
        fromMemberProfile: 'member-1',
        currentCoach: SessionPackageModel(id: 'p0', trainerId: 'coach-1', trainerName: 'Omar', remainingSessions: 3),
      ),
    );

    expect(find.byKey(const ValueKey('locked-coach')), findsOneWidget);
    expect(find.text('Coach Omar'), findsOneWidget);
    expect(find.byKey(const ValueKey('trainer-dropdown')), findsNothing);
  });

  testWidgets('member, coach and package → confirm → POST without assignedBy', (tester) async {
    final t = await _pump(tester, const _Desk(fromMemberProfile: 'member-1'));

    expect(_canAssign(tester), isFalse);
    await tester.tap(find.byKey(const ValueKey('trainer-dropdown')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Omar Coach').last);
    await tester.pumpAndSettle();
    await tester.tap(find.byKey(const ValueKey('template-tpl-12')));
    await tester.pumpAndSettle();
    expect(find.textContaining('150'), findsWidgets, reason: 'per-session price is shown');
    expect(_canAssign(tester), isTrue);

    await tester.tap(find.byKey(const ValueKey('assign-package')));
    await tester.pumpAndSettle();
    expect(find.textContaining('12 sessions with Coach Omar Coach for Mona Ali'), findsOneWidget);
    await tester.tap(find.text('Confirm'));
    await tester.pumpAndSettle();

    final body = t.adapter.recorded.firstWhere((r) => r.method == 'POST').bodyAsMap!;
    expect(body, {'gymId': 'gym-1', 'memberId': 'member-1', 'trainerId': 'coach-1', 'templateId': 'tpl-12'});
    expect(find.text('Package assigned'), findsOneWidget);
    expect(find.text('open'), findsOneWidget);
  });
}
