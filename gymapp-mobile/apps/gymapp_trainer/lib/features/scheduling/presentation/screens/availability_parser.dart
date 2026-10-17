import 'package:flutter/material.dart';

/// Pure parsing helpers for the AvailabilityScreen hydration path.
/// Extracted so the BE-shape contract (string `dayOfWeek`, "HH:mm:ss" times)
/// is exercised in unit tests without spinning up the widget tree.
class AvailabilityParser {
  AvailabilityParser._();

  static const Map<String, int> dayNameToIndex = {
    'sunday': 0,
    'monday': 1,
    'tuesday': 2,
    'wednesday': 3,
    'thursday': 4,
    'friday': 5,
    'saturday': 6,
  };

  /// Coerces the BE's `dayOfWeek` field — string ("monday") or int (1) — into
  /// our 0-Sunday … 6-Saturday convention. Returns null for unknown values.
  static int? parseDayOfWeek(dynamic value) {
    if (value is int) return _validateIndex(value);
    if (value is num) return _validateIndex(value.toInt());
    if (value is String) {
      final byName = dayNameToIndex[value.toLowerCase()];
      if (byName != null) return byName;
      final byInt = int.tryParse(value);
      if (byInt != null) return _validateIndex(byInt);
    }
    return null;
  }

  static int? _validateIndex(int v) => (v >= 0 && v <= 6) ? v : null;

  /// Parses a "HH:mm" or "HH:mm:ss" string into a [TimeOfDay]. Returns null
  /// for malformed input — caller should skip the slot rather than throw.
  static TimeOfDay? parseTime(dynamic value) {
    if (value is! String) return null;
    final parts = value.split(':');
    if (parts.length < 2) return null;
    final h = _parseInRange(parts[0], 23);
    final m = _parseInRange(parts[1], 59);
    if (h == null || m == null) return null;
    return TimeOfDay(hour: h, minute: m);
  }

  /// Parses [part] as an int in `0..max`, or null when malformed/out of range.
  static int? _parseInRange(String part, int max) {
    final v = int.tryParse(part);
    return (v != null && v >= 0 && v <= max) ? v : null;
  }
}
