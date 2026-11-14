/// Reads the values of a printed InBody result sheet (270 / 370 / 570 / 770) from OCR text.
///
/// Pure Dart: the OCR engine is an adapter that turns its output into [OcrLine]s, so this file
/// can be unit-tested with text fixtures. Fields that can't be read are left null; the member
/// always reviews and edits the values before they are saved.
library;

/// One line of recognised text with its position on the photo (pixels).
class OcrLine {
  final String text;
  final double top;
  final double bottom;
  final double left;

  const OcrLine(this.text, {required this.top, required this.bottom, required this.left});

  double get centerY => (top + bottom) / 2;
  double get height => bottom - top;
}

/// How sure the parser is about a value. [low] values are highlighted for the member to check.
enum ReadConfidence { high, low }

/// A value read from the sheet.
class ReadValue {
  final double value;
  final ReadConfidence confidence;

  const ReadValue(this.value, this.confidence);

  bool get isLow => confidence == ReadConfidence.low;
}

/// Everything the parser could read. Masses in kg, height in cm.
class InBodyReading {
  final ReadValue? weightKg;
  final ReadValue? heightCm;
  final ReadValue? bodyFatPercent;
  final ReadValue? skeletalMuscleMassKg;
  final ReadValue? bodyWaterKg;
  final DateTime? testDate;

  const InBodyReading({
    this.weightKg,
    this.heightCm,
    this.bodyFatPercent,
    this.skeletalMuscleMassKg,
    this.bodyWaterKg,
    this.testDate,
  });

  /// True when nothing usable was found, e.g. the photo is not an InBody sheet.
  bool get isEmpty =>
      weightKg == null &&
      heightCm == null &&
      bodyFatPercent == null &&
      skeletalMuscleMassKg == null &&
      bodyWaterKg == null;
}

/// One field: the labels that name it on the sheet, the row words that rule a row out, and the
/// range a real value falls in.
class _Field {
  final List<RegExp> labels;
  final List<String> excludeRowsWith;
  final double min;
  final double max;

  /// Whether a whole number (no decimals) counts as a confident read. InBody prints most values
  /// with one decimal, while the bar-chart scale beside them is whole numbers.
  final bool wholeNumberIsConfident;

  _Field(
    List<String> labels, {
    required this.min,
    required this.max,
    this.excludeRowsWith = const [],
    this.wholeNumberIsConfident = false,
  }) : labels = labels.map((l) => RegExp(l, caseSensitive: false)).toList();
}

final _weight = _Field(
  [r'\bweight\b'],
  min: 20,
  max: 300,
  excludeRowsWith: ['target', 'control', 'ideal'],
);
final _height = _Field([r'\bheight\b'], min: 100, max: 230, wholeNumberIsConfident: true);
final _bodyFat = _Field(
  [r'percent\s*body\s*fat', r'\bPBF\b'],
  min: 3,
  max: 70,
);
final _muscle = _Field(
  [r'skeletal\s*muscle\s*mass', r'\bSMM\b'],
  min: 5,
  max: 80,
);
final _water = _Field(
  [r'total\s*body\s*water', r'\bTBW\b'],
  min: 10,
  max: 90,
  // ECW/TBW is a ratio (≈0.38), not the water mass.
  excludeRowsWith: ['ecw'],
);

const _kgPerPound = 0.45359237;

/// Ranges like "(55.0~74.4)" or "55.0 - 74.4" are the normal range, never the member's value.
final _normalRange = RegExp(r'\([^)]*\)|\d+(?:\.\d+)?\s*[~–-]\s*\d+(?:\.\d+)?');
final _number = RegExp(r'\d+(?:\.\d+)?');
final _date = RegExp(r'(20\d{2})[./-](\d{1,2})[./-](\d{1,2})');
final _pounds = RegExp(r'\blbs?\b', caseSensitive: false);

class InBodySheetParser {
  const InBodySheetParser();

  InBodyReading parse(List<OcrLine> lines) {
    final rows = groupIntoRows(lines);

    return InBodyReading(
      weightKg: _read(rows, _weight),
      heightCm: _read(rows, _height),
      bodyFatPercent: _read(rows, _bodyFat),
      skeletalMuscleMassKg: _read(rows, _muscle),
      bodyWaterKg: _read(rows, _water),
      testDate: _readDate(rows),
    );
  }

  /// Puts lines that sit at the same height on the photo into one row, left to right, so a label
  /// and its value in another column end up in the same string.
  static List<String> groupIntoRows(List<OcrLine> lines) {
    final sorted = [...lines]..sort((a, b) => a.centerY.compareTo(b.centerY));
    final rows = <List<OcrLine>>[];

    for (final line in sorted) {
      final row = rows.isEmpty ? null : rows.last;
      if (row != null && _sameRow(row.first, line)) {
        row.add(line);
      } else {
        rows.add([line]);
      }
    }

    return rows.map((row) {
      row.sort((a, b) => a.left.compareTo(b.left));
      return _normalize(row.map((l) => l.text).join(' '));
    }).toList();
  }

  static bool _sameRow(OcrLine anchor, OcrLine line) {
    final tolerance = (anchor.height < line.height ? anchor.height : line.height) / 2;
    return (anchor.centerY - line.centerY).abs() <= tolerance;
  }

  /// Decimal commas ("80,1") become points.
  static String _normalize(String text) => text.replaceAllMapped(RegExp(r'(\d),(\d)'), (m) => '${m[1]}.${m[2]}');

  /// The first confident value for [field] on the sheet, else the first plausible one (low).
  static ReadValue? _read(List<String> rows, _Field field) {
    ReadValue? fallback;

    for (final row in rows) {
      final value = _readRow(row, field);
      if (value == null) continue;
      if (!value.isLow) return value;
      fallback ??= value;
    }

    return fallback;
  }

  static ReadValue? _readRow(String row, _Field field) {
    final lower = row.toLowerCase();
    if (field.excludeRowsWith.any(lower.contains)) return null;

    final afterLabel = _textAfterLabel(row, field);
    if (afterLabel == null) return null;

    final factor = _pounds.hasMatch(afterLabel) ? _kgPerPound : 1.0;
    final candidates = _withoutScaleTicks(afterLabel.replaceAll(_normalRange, ' '))
        .map((raw) => (raw: raw, value: double.parse(raw) * factor))
        .where((c) => c.value >= field.min && c.value <= field.max)
        .toList();
    if (candidates.isEmpty) return null;

    final decimal = candidates.where((c) => c.raw.contains('.'));
    if (decimal.isNotEmpty) return ReadValue(_round1(decimal.first.value), ReadConfidence.high);

    final confident = field.wholeNumberIsConfident || candidates.length == 1;
    return ReadValue(_round1(candidates.first.value), confident ? ReadConfidence.high : ReadConfidence.low);
  }

  /// The numbers in [text], minus bar-chart scale ticks: any run of 3+ consecutive numbers with
  /// the same step (e.g. "8.0 13.0 18.0 23.0" or "55 70 85 100"). The member's own value breaks
  /// the step, so it survives.
  static List<String> _withoutScaleTicks(String text) {
    final raw = _number.allMatches(text).map((m) => m[0]!).toList();
    final values = raw.map(double.parse).toList();
    final isTick = List.filled(raw.length, false);

    for (var i = 1; i + 1 < values.length; i++) {
      final step = values[i] - values[i - 1];
      if (step > 0 && (values[i + 1] - values[i] - step).abs() < 1e-6) {
        isTick[i - 1] = isTick[i] = isTick[i + 1] = true;
      }
    }

    return [for (var i = 0; i < raw.length; i++) if (!isTick[i]) raw[i]];
  }

  static String? _textAfterLabel(String row, _Field field) {
    for (final label in field.labels) {
      final match = label.firstMatch(row);
      if (match != null) return row.substring(match.end);
    }
    return null;
  }

  static DateTime? _readDate(List<String> rows) {
    for (final row in rows) {
      final date = _dateIn(row);
      if (date != null) return date;
    }
    return null;
  }

  /// The first real calendar date (yyyy.mm.dd, yyyy-mm-dd or yyyy/mm/dd) in [row], if any.
  static DateTime? _dateIn(String row) {
    final m = _date.firstMatch(row);
    if (m == null) return null;

    final (year, month, day) = (int.parse(m[1]!), int.parse(m[2]!), int.parse(m[3]!));
    final date = DateTime(year, month, day);
    // DateTime rolls invalid dates over (month 13 → next year); only keep exact matches.
    final isReal = date.month == month && date.day == day;
    return isReal ? date : null;
  }

  static double _round1(double v) => (v * 10).round() / 10;
}
