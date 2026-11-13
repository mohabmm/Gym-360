import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_gym/core/providers.dart';
import 'package:gymapp_gym/features/members/presentation/screens/add_member_screen.dart';
import 'package:gymapp_gym/features/members/presentation/widgets/assign_trainer_form.dart';
import 'package:gymapp_gym/features/trainers/presentation/providers/trainers_provider.dart';

/// The staff app is the controller: a member is added together with a required coach
/// and any number of sessions.
/// Logged-in desk user with the given staff role.
class _FakeAuth extends AuthNotifier {
  final String role;
  _FakeAuth(this.role);

  @override
  Future<AuthState> build() async => AuthState(status: AuthStatus.authenticated, staffRole: role);
}

void main() {
  const coach = UserModel(id: 'coach-1', email: 'c@gym.eg', firstName: 'Omar', lastName: 'Coach', role: 'trainer', status: 'active');

  group('validateSessionCount', () {
    String? check(String v, {bool trial = false}) => validateSessionCount(v, isTrial: trial, isArabic: false);

    test('accepts any number from 1 to 100', () {
      expect(check('1'), isNull);
      expect(check('7'), isNull);
      expect(check('100'), isNull);
    });

    test('rejects empty, zero and more than 100', () {
      expect(check(''), isNotNull);
      expect(check('0'), isNotNull);
      expect(check('101'), isNotNull);
    });

    test('a free trial stays at 1 to 3', () {
      expect(check('3', trial: true), isNull);
      expect(check('4', trial: true), isNotNull);
    });
  });

  group('quickAssignPayload', () {
    AssignTrainerDraft draft({String sessions = '7', String price = ''}) => AssignTrainerDraft()
      ..trainerId = coach.id
      ..sessions.text = sessions
      ..price.text = price;

    Map<String, dynamic> body(AssignTrainerDraft d) =>
        quickAssignPayload((gymId: 'g1', memberId: 'm1', assignedBy: 'desk-1'), d);

    test('free sessions send the coach and count with no price or payment', () {
      expect(body(draft()), {
        'gymId': 'g1',
        'trainerId': 'coach-1',
        'memberId': 'm1',
        'sessionCount': 7,
        'assignedBy': 'desk-1',
      });
    });

    test('a priced package also sends the price and payment method', () {
      final paid = body(draft(sessions: '6', price: '1200'));
      expect(paid['totalPrice'], 1200);
      expect(paid['paymentMethod'], 'Cash');
    });
  });

  group('AddMemberScreen', () {
    Future<void> pump(WidgetTester tester, List<UserModel> trainers, {String role = 'owner'}) async {
      tester.view.physicalSize = const Size(1080, 2400);
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.reset);
      await tester.pumpWidget(ProviderScope(
        overrides: [
          trainersProvider.overrideWith((ref) async => trainers),
          authProvider.overrideWith(() => _FakeAuth(role)),
        ],
        child: const MaterialApp(home: AddMemberScreen()),
      ));
      await tester.pumpAndSettle();
    }

    testWidgets('coach and number of sessions are required', (tester) async {
      await pump(tester, const [coach]);

      expect(find.text('Coach and sessions'), findsOneWidget);
      await tester.tap(find.widgetWithText(AppButton, 'Add Member'));
      await tester.pumpAndSettle();

      expect(find.text('Pick a coach'), findsWidgets);
      expect(find.text('Enter a number from 1 to 100'), findsOneWidget);
    });

    testWidgets('a member cannot be added while the gym has no approved coach', (tester) async {
      await pump(tester, const []);

      expect(find.text(noCoachesText(false)), findsOneWidget);
      final button = tester.widget<AppButton>(find.widgetWithText(AppButton, 'Add Member'));
      expect(button.onPressed, isNull);
    });

    testWidgets('shows the fixed first password the member signs in with', (tester) async {
      await pump(tester, const [coach]);

      expect(find.text("A new member's first password: 12345678"), findsOneWidget);
    });

    testWidgets('with no coach, an owner can add one right from the form', (tester) async {
      await pump(tester, const []);

      expect(find.widgetWithText(OutlinedButton, 'Add coach'), findsOneWidget);
    });

    testWidgets('a receptionist cannot add coaches, so sees only the message', (tester) async {
      await pump(tester, const [], role: 'receptionist');

      expect(find.text(noCoachesText(false)), findsOneWidget);
      expect(find.text('Add coach'), findsNothing);
    });

    testWidgets('with coaches, a manager also gets a New coach shortcut', (tester) async {
      await pump(tester, const [coach], role: 'manager');

      expect(find.widgetWithText(TextButton, 'New coach'), findsOneWidget);
    });
  });
}
