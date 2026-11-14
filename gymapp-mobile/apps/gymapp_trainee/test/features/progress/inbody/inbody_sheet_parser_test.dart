import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_trainee/features/progress/data/inbody/inbody_sheet_parser.dart';

/// One printed row: the pieces ML Kit returns as separate lines at the same height, with their x.
List<OcrLine> _row(double y, List<(double, String)> pieces) =>
    [for (final (x, text) in pieces) OcrLine(text, top: y, bottom: y + 18, left: x)];

/// What ML Kit returns for a typical InBody 270 sheet (English), top to bottom.
final _inBody270 = [
  ..._row(10, [(0, 'InBody')]),
  ..._row(40, [
    (0, 'ID 7824'), (150, 'Height'), (220, '178cm'), (320, 'Age 32'),
    (420, 'Gender Male'), (560, 'Test Date / Time'), (700, '2026.11.10. 09:41'),
  ]),
  // Body Composition Analysis
  ..._row(100, [(0, 'Total Body Water (L)'), (200, '45.2'), (260, '(38.0~46.4)')]),
  ..._row(130, [(0, 'Protein (kg)'), (200, '12.2'), (260, '(10.2~12.4)')]),
  ..._row(160, [(0, 'Minerals (kg)'), (200, '4.13'), (260, '(3.52~4.30)')]),
  ..._row(190, [(0, 'Body Fat Mass (kg)'), (200, '14.9'), (260, '(8.2~16.5)')]),
  ..._row(220, [(0, 'Weight (kg)'), (200, '80.1'), (260, '(57.2~77.4)')]),
  // Muscle-Fat Analysis: scale ticks share the row with the value
  ..._row(280, [(0, 'Weight'), (80, 'kg'), (120, '55 70 85 100 115 130 145 160 175 190 205'), (600, '%')]),
  ..._row(330, [
    (0, 'SMM'), (40, 'Skeletal Muscle Mass'), (200, 'kg'),
    (240, '70 80 90 100 110 120 130 140 150 160 170'), (560, '35.2'),
  ]),
  // Obesity Analysis: the PBF / BMI scales are printed with decimals
  ..._row(400, [
    (0, 'BMI'), (40, 'Body Mass Index'), (200, 'kg/m²'),
    (240, '10.0 15.0 18.5 21.0 25.0 30.0 35.0 40.0 45.0 50.0 55.0'), (600, '25.3'),
  ]),
  ..._row(430, [
    (0, 'PBF'), (40, 'Percent Body Fat'), (200, '%'),
    (240, '8.0 13.0 18.0 23.0 28.0 33.0 38.0 43.0 48.0 53.0 58.0'), (600, '18.6'),
  ]),
  // Weight Control and research parameters must not be read as weight / water
  ..._row(500, [(0, 'Target Weight'), (150, '76.3 kg')]),
  ..._row(520, [(0, 'Weight Control'), (150, '- 3.8 kg')]),
  ..._row(560, [(0, 'ECW/TBW'), (150, '0.38')]),
];

void main() {
  const parser = InBodySheetParser();

  test('reads every value from an InBody 270 sheet, ignoring ranges, scales and weight control', () {
    final r = parser.parse(_inBody270);

    expect(r.weightKg?.value, 80.1);
    expect(r.heightCm?.value, 178);
    expect(r.bodyFatPercent?.value, 18.6);
    expect(r.skeletalMuscleMassKg?.value, 35.2);
    expect(r.bodyWaterKg?.value, 45.2);
    expect(r.testDate, DateTime(2026, 11, 10));
    for (final v in [r.weightKg, r.heightCm, r.bodyFatPercent, r.skeletalMuscleMassKg, r.bodyWaterKg]) {
      expect(v!.isLow, isFalse);
    }
  });

  test('label and value read as separate lines a few pixels apart land in one row', () {
    final r = parser.parse(const [
      OcrLine('Weight', top: 100, bottom: 120, left: 0),
      OcrLine('80.1', top: 104, bottom: 122, left: 300),
      OcrLine('Height', top: 160, bottom: 180, left: 0),
      OcrLine('175.5 cm', top: 157, bottom: 177, left: 300),
    ]);

    expect(r.weightKg?.value, 80.1);
    expect(r.heightCm?.value, 175.5);
  });

  test('decimal commas, partial sheets and ECW/TBW (570 / 770)', () {
    final r = parser.parse([
      ..._row(0, [(0, 'Weight'), (200, '80,1 kg')]),
      ..._row(40, [(0, 'Percent Body Fat'), (200, '18,6 %')]),
      ..._row(80, [(0, 'ECW/TBW'), (200, '0.380')]),
      ..._row(120, [(0, 'TBW Total Body Water'), (200, '45,2 L')]),
    ]);

    expect(r.weightKg?.value, 80.1);
    expect(r.bodyFatPercent?.value, 18.6);
    expect(r.bodyWaterKg?.value, 45.2);
    expect(r.heightCm, isNull);
    expect(r.skeletalMuscleMassKg, isNull);
    expect(r.testDate, isNull);
  });

  test('weight printed in pounds is converted to kg', () {
    final r = parser.parse(_row(0, [(0, 'Weight (lb)'), (200, '176.6')]));

    expect(r.weightKg?.value, 80.1);
  });

  test('an unclear whole number is kept but flagged for the member to check', () {
    final r = parser.parse(_row(0, [(0, 'Weight'), (100, '80 kg'), (300, '95')]));

    expect(r.weightKg?.value, 80);
    expect(r.weightKg!.isLow, isTrue);
  });

  test('values outside a real range are not read', () {
    final r = parser.parse([
      ..._row(0, [(0, 'Height'), (100, '18 cm')]),
      ..._row(40, [(0, 'Percent Body Fat'), (100, '96.0')]),
    ]);

    expect(r.heightCm, isNull);
    expect(r.bodyFatPercent, isNull);
  });

  test('a photo that is not an InBody sheet reads as empty', () {
    final r = parser.parse(_row(0, [(0, 'Receipt'), (100, 'Total 250.00 EGP'), (300, '2026-13-40')]));

    expect(r.isEmpty, isTrue);
    expect(r.testDate, isNull);
  });
}
