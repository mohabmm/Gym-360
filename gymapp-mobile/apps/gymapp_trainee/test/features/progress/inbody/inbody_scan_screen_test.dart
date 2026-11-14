import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:gymapp_trainee/features/progress/data/inbody/inbody_scan_request.dart';
import 'package:gymapp_trainee/features/progress/data/inbody/inbody_sheet_parser.dart';
import 'package:gymapp_trainee/features/progress/data/inbody/inbody_text_reader.dart';
import 'package:gymapp_trainee/features/progress/data/inbody/inbody_scan_api.dart';
import 'package:gymapp_trainee/features/progress/presentation/screens/inbody_scan_screen.dart';

class _FakeReader implements InBodyTextReader {
  final InBodyReading reading;
  String? readPath;

  _FakeReader(this.reading);

  @override
  Future<InBodyReading> read(String imagePath) async {
    readPath = imagePath;
    return reading;
  }
}

class _FakeApi implements InBodyScanApi {
  Map<String, dynamic>? saved;

  @override
  Future<void> save(InBodyScanRequest request) async => saved = request.toJson();

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

const _sheet = InBodyReading(
  weightKg: ReadValue(80.1, ReadConfidence.high),
  heightCm: ReadValue(178, ReadConfidence.high),
  bodyFatPercent: ReadValue(18.6, ReadConfidence.high),
  skeletalMuscleMassKg: ReadValue(35, ReadConfidence.low),
  bodyWaterKg: null,
);

Future<void> _open(WidgetTester tester, {required InBodyTextReader reader, required InBodyScanApi repo, String locale = 'en'}) async {
  tester.view.physicalSize = const Size(1080, 4000);
  tester.view.devicePixelRatio = 2.5;
  addTearDown(tester.view.reset);

  final router = GoRouter(routes: [
    GoRoute(
      path: '/',
      builder: (context, _) => Scaffold(
        body: TextButton(onPressed: () => context.push('/inbody'), child: const Text('open')),
      ),
    ),
    GoRoute(path: '/inbody', builder: (_, __) => const InBodyScanScreen()),
  ]);

  await tester.pumpWidget(ProviderScope(
    overrides: [
      inBodyPhotoPickerProvider.overrideWithValue((_) async => '/tmp/sheet.jpg'),
      inBodyTextReaderProvider.overrideWithValue(reader),
      inBodyScanApiProvider.overrideWithValue(repo),
    ],
    child: MaterialApp.router(
      routerConfig: router,
      locale: Locale(locale),
      supportedLocales: const [Locale('ar'), Locale('en')],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
    ),
  ));
  await tester.tap(find.text('open'));
  await tester.pumpAndSettle();
}

Finder _field(String key) => find.byKey(ValueKey('inbody-$key'));

void main() {
  testWidgets('scan pre-fills the form, flags unsure values and saves what the member confirmed', (tester) async {
    final reader = _FakeReader(_sheet);
    final repo = _FakeApi();
    await _open(tester, reader: reader, repo: repo);

    await tester.tap(find.text('Take photo'));
    await tester.pumpAndSettle();

    expect(reader.readPath, '/tmp/sheet.jpg');
    expect(tester.widget<TextField>(_field('weightKg')).controller!.text, '80.1');
    expect(tester.widget<TextField>(_field('heightCm')).controller!.text, '178.0');
    // Low-confidence muscle and the missing water value both ask the member to check.
    expect(find.text('Please check this value'), findsNWidgets(2));

    await tester.enterText(_field('skeletalMuscleMassKg'), '35.2');
    await tester.enterText(_field('bodyWaterKg'), '45.2');
    await tester.tap(find.text('Save results'));
    await tester.pumpAndSettle();

    expect(repo.saved, {
      'date': isA<String>(),
      'weightKg': 80.1,
      'heightCm': 178.0,
      'bodyFatPercentage': 18.6,
      'skeletalMuscleMassKg': 35.2,
      'bodyWaterKg': 45.2,
    });
    expect(find.byType(InBodyScanScreen), findsNothing, reason: 'screen closes after saving');
  });

  testWidgets('unreadable photo still opens an empty form the member can fill in', (tester) async {
    final repo = _FakeApi();
    await _open(tester, reader: _FakeReader(const InBodyReading()), repo: repo);

    await tester.tap(find.text('Choose from gallery'));
    await tester.pumpAndSettle();

    expect(find.textContaining("Couldn't read the sheet"), findsOneWidget);
    await tester.tap(find.text('Save results'));
    await tester.pumpAndSettle();

    expect(repo.saved, isNull);
    expect(find.text('Enter at least one value'), findsOneWidget);
  });

  testWidgets('Arabic: screen is in Arabic', (tester) async {
    await _open(tester, reader: _FakeReader(_sheet), repo: _FakeApi(), locale: 'ar');

    expect(find.text('التقط صورة'), findsOneWidget);
    expect(find.text('اختر من المعرض'), findsOneWidget);
  });
}
