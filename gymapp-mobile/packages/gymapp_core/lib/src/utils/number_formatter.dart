import 'package:intl/intl.dart';

import '../types/app_language.dart';
import '../types/measurements.dart';

/// Number formatting utilities with Arabic locale support.
/// Supports Arabic-Indic numerals when needed.
class NumberFormatter {
  NumberFormatter._();

  /// Format number with locale-appropriate separators.
  static String format(
    num number, {
    AppLanguage language = AppLanguage.arabic,
  }) {
    return NumberFormat('#,##0', language.code).format(number);
  }

  /// Format as currency (EGP).
  static String formatCurrency(
    Egp amount, {
    AppLanguage language = AppLanguage.arabic,
  }) {
    if (language.isArabic) {
      return '${format(amount, language: language)} ج.م';
    }
    return 'EGP ${format(amount, language: language)}';
  }

  /// Format as percentage.
  static String formatPercentage(Percent value, {int decimals = 1}) {
    return '${value.toStringAsFixed(decimals)}%';
  }

  /// Format weight (kg or lbs).
  static String formatWeight(
    double value, {
    WeightUnit unit = WeightUnit.kg,
    int decimals = 1,
  }) {
    return '${value.toStringAsFixed(decimals)} ${unit.symbol}';
  }

  /// Format height (cm).
  static String formatHeight(Centimeters value, {int decimals = 0}) {
    return '${value.toStringAsFixed(decimals)} cm';
  }

  /// Format with sign (+/-).
  static String formatWithSign(double value, {int decimals = 1}) {
    final prefix = value > 0 ? '+' : '';
    return '$prefix${value.toStringAsFixed(decimals)}';
  }

  /// Format compact number (1.2K, 3.5M etc.).
  static String formatCompact(
    num number, {
    AppLanguage language = AppLanguage.english,
  }) {
    return NumberFormat.compact(locale: language.code).format(number);
  }

  /// Format water amount in ml/L.
  static String formatWater(
    Milliliters ml, {
    AppLanguage language = AppLanguage.arabic,
  }) {
    if (ml >= 1000) {
      final liters = ml / 1000;
      final formatted =
          liters.toStringAsFixed(liters.truncateToDouble() == liters ? 0 : 1);
      return language.isArabic ? '$formatted لتر' : '${formatted}L';
    }
    return language.isArabic ? '$ml مل' : '${ml}ml';
  }

  /// Format calories.
  static String formatCalories(
    Kilocalories calories, {
    AppLanguage language = AppLanguage.arabic,
  }) {
    final number = format(calories, language: language);
    return language.isArabic ? '$number سعرة' : '$number cal';
  }

  /// Format rating (e.g., "4.7").
  static String formatRating(StarRating rating) {
    return rating.toStringAsFixed(1);
  }

  /// Format ordinal number (1st, 2nd, 3rd... or Arabic ordinals).
  static String formatOrdinal(
    int number, {
    AppLanguage language = AppLanguage.arabic,
  }) {
    if (language.isArabic) {
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
    SessionUsage usage, {
    AppLanguage language = AppLanguage.arabic,
  }) {
    final count = '${usage.used}/${usage.total}';
    return language.isArabic ? '$count جلسة' : '$count sessions';
  }
}
