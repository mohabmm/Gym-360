import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_core/gymapp_core.dart';
import 'package:gymapp_gym/core/staff_created_account.dart';
import 'package:gymapp_gym/features/trainers/presentation/screens/add_trainer_screen.dart';

class _Call {
  final String path;
  final dynamic data;
  _Call(this.path, this.data);
}

/// Records POSTs; answers the create-trainer call with a user id and [initialPassword].
class _FakeApi implements ApiClient {
  final calls = <_Call>[];
  final String? initialPassword;
  _FakeApi({this.initialPassword = '12345678'});

  @override
  Future<T> post<T>(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
    required T Function(dynamic data) parser,
    dynamic cancelToken,
  }) async {
    calls.add(_Call(path, data));
    return parser(path == ApiEndpoints.identityStaffCreateTrainer
        ? {'userId': 'u-1', 'initialPassword': initialPassword}
        : null);
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

Widget _app(Locale locale) => ProviderScope(
      child: MaterialApp(
        locale: locale,
        supportedLocales: const [Locale('ar'), Locale('en')],
        localizationsDelegates: const [
          GlobalMaterialLocalizations.delegate,
          GlobalWidgetsLocalizations.delegate,
          GlobalCupertinoLocalizations.delegate,
        ],
        home: const AddTrainerScreen(),
      ),
    );

void main() {
  test('creates the trainer account, then adds them to the gym', () async {
    final api = _FakeApi();
    final created = await createTrainerForGym(api,
        gymId: 'g-1',
        trainer: const NewTrainer(firstName: 'Omar', lastName: 'Ali', phone: '01012345678', email: ''));

    expect(api.calls.map((c) => c.path), [
      ApiEndpoints.identityStaffCreateTrainer,
      ApiEndpoints.gymTrainers(const GymId('g-1')),
    ]);
    expect(api.calls.first.data, {
      'firstName': 'Omar',
      'lastName': 'Ali',
      'phone': '01012345678',
      'email': null,
    });
    expect(api.calls.last.data, {'trainerId': 'u-1'});
    expect(created.userId, 'u-1');
    expect(created.initialPassword, '12345678');
  });

  test('an existing trainer comes back without a password (theirs is unchanged)', () async {
    final created = await createTrainerForGym(_FakeApi(initialPassword: null),
        gymId: 'g-1', trainer: const NewTrainer(firstName: 'Omar', lastName: 'Ali', phone: '01012345678'));

    expect(created.userId, 'u-1');
    expect(created.initialPassword, isNull);
  });

  test('the default first password is 12345678', () {
    expect(kStaffCreatedAccountPassword, '12345678');
  });

  testWidgets('empty form shows Arabic required errors and sends nothing', (tester) async {
    await tester.pumpWidget(_app(const Locale('ar')));
    expect(find.text('اضافة مدرب'), findsOneWidget);

    await tester.tap(find.text('اضافة المدرب'));
    await tester.pump();

    expect(find.text('مطلوب'), findsNWidgets(3));
  });

  testWidgets('short phone and bad email are rejected in English', (tester) async {
    await tester.pumpWidget(_app(const Locale('en')));
    final fields = find.byType(TextFormField);
    await tester.enterText(fields.at(0), 'Omar');
    await tester.enterText(fields.at(1), 'Ali');
    await tester.enterText(fields.at(2), '0101');
    await tester.enterText(fields.at(3), 'not-an-email');

    await tester.tap(find.byType(AppButton));
    await tester.pump();

    expect(find.text('Phone number is too short'), findsOneWidget);
    expect(find.text('Invalid email address'), findsOneWidget);
  });
}
