import 'package:intl/intl.dart';

/// Number formatting utilities with Arabic locale support.
/// Supports Arabic-Indic numerals when needed.
class NumberFormatter {
  NumberFormatter._();

  /// Format number with locale-appropriate separators.
  static String format(num number, {String locale = 'ar'}) {
    return NumberFormat('#,##0', locale).format(number);
  }

  /// Format as currency (EGP).
  static String formatCurrency(
    double amount, {
    String locale = 'ar',
    String currency = 'EGP',
  }) {
    if (locale == 'ar') {
      return '${format(amount, locale: locale)} ج.م';
    }
    return '$currency ${format(amount, locale: locale)}';
  }

  /// Format as percentage.
  static String formatPercentage(double value, {int decimals = 1}) {
    return '${value.toStringAsFixed(decimals)}%';
  }

  /// Format weight (kg or lbs).
  static String formatWeight(
    double value, {
    String unit = 'kg',
    int decimals = 1,
  }) {
    return '${value.toStringAsFixed(decimals)} $unit';
  }

  /// Format height (cm).
  static String formatHeight(double value, {int decimals = 0}) {
    return '${value.toStringAsFixed(decimals)} cm';
  }

  /// Format with sign (+/-).
  static String formatWithSign(double value, {int decimals = 1}) {
    final prefix = value > 0 ? '+' : '';
    return '$prefix${value.toStringAsFixed(decimals)}';
  }

  /// Format compact number (1.2K, 3.5M etc.).
  static String formatCompact(num number, {String locale = 'en'}) {
    return NumberFormat.compact(locale: locale).format(number);
  }

  /// Format water amount in ml/L.
  static String formatWater(int ml, {String locale = 'ar'}) {
    if (ml >= 1000) {
      final liters = ml / 1000;
      final formatted = liters.toStringAsFixed(liters.truncateToDouble() == liters ? 0 : 1);
      return locale == 'ar' ? '$formatted لتر' : '${formatted}L';
    }
    return locale == 'ar' ? '$ml مل' : '${ml}ml';
  }

  /// Format calories.
  static String formatCalories(int calories, {String locale = 'ar'}) {
    return locale == 'ar'
        ? '${format(calories, locale: locale)} سعرة'
        : '${format(calories, locale: locale)} cal';
  }

  /// Format rating (e.g., "4.7").
  static String formatRating(double rating) {
    return rating.toStringAsFixed(1);
  }

  /// Format ordinal number (1st, 2nd, 3rd... or Arabic ordinals).
  static String formatOrdinal(int number, {String locale = 'ar'}) {
    if (locale == 'ar') {
      return '#$number';
    }
    if (number % 100 >= 11 && number % 100 <= 13) {
      return '${number}th';
    }
    switch (number % 10) {
      case 1:
        return '${number}st';
      case 2:
        return '${number}nd';
      case 3:
        return '${number}rd';
      default:
        return '${number}th';
    }
  }

  /// Format session count (e.g., "5/12 sessions").
  static String formatSessionCount(
    int used,
    int total, {
    String locale = 'ar',
  }) {
    return locale == 'ar' ? '$used/$total جلسة' : '$used/$total sessions';
  }
}
