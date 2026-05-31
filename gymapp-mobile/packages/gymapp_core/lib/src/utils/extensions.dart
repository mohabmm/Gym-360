import 'package:flutter/material.dart';

/// Extension methods for common types used throughout the app.

extension StringExtensions on String {
  /// Capitalize the first letter.
  String capitalize() {
    if (isEmpty) return this;
    return '${this[0].toUpperCase()}${substring(1)}';
  }

  /// Convert hex string to Color. Returns transparent for malformed input
  /// rather than throwing — callers can branch on `Color(0)` if they care.
  Color toColor() {
    final clean = replaceFirst('#', '').trim();
    if (!RegExp(r'^[0-9A-Fa-f]{6}([0-9A-Fa-f]{2})?$').hasMatch(clean)) {
      return const Color(0x00000000);
    }
    final buffer = StringBuffer();
    if (clean.length == 6) buffer.write('FF');
    buffer.write(clean);
    final parsed = int.tryParse(buffer.toString(), radix: 16);
    return parsed != null ? Color(parsed) : const Color(0x00000000);
  }

  /// Check if string is a valid email.
  bool get isEmail =>
      RegExp(r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$')
          .hasMatch(this);

  /// Check if string is a valid phone number.
  bool get isPhone =>
      RegExp(r'^(\+?2)?0[0-9]{10}$').hasMatch(replaceAll(RegExp(r'[\s-]'), ''));

  /// Truncate with ellipsis.
  String truncate(int maxLength) {
    if (length <= maxLength) return this;
    return '${substring(0, maxLength)}...';
  }

  /// Get initials from a name.
  String get initials {
    final parts = trim().split(' ');
    if (parts.isEmpty) return '';
    if (parts.length == 1) return parts.first[0].toUpperCase();
    return '${parts.first[0]}${parts.last[0]}'.toUpperCase();
  }
}

extension DateTimeExtensions on DateTime {
  /// Check if this date is today.
  bool get isToday {
    final now = DateTime.now();
    return year == now.year && month == now.month && day == now.day;
  }

  /// Check if this date is tomorrow.
  bool get isTomorrow {
    final tomorrow = DateTime.now().add(const Duration(days: 1));
    return year == tomorrow.year &&
        month == tomorrow.month &&
        day == tomorrow.day;
  }

  /// Check if this date is yesterday.
  bool get isYesterday {
    final yesterday = DateTime.now().subtract(const Duration(days: 1));
    return year == yesterday.year &&
        month == yesterday.month &&
        day == yesterday.day;
  }

  /// Check if this date is in the past.
  bool get isPast => isBefore(DateTime.now());

  /// Check if this date is in the future.
  bool get isFuture => isAfter(DateTime.now());

  /// Get start of day.
  DateTime get startOfDay => DateTime(year, month, day);

  /// Get end of day.
  DateTime get endOfDay => DateTime(year, month, day, 23, 59, 59);

  /// Get start of week (Saturday for Arabic/Egyptian calendar).
  DateTime get startOfWeek {
    final daysFromSaturday = (weekday + 1) % 7;
    return subtract(Duration(days: daysFromSaturday)).startOfDay;
  }

  /// Get start of month.
  DateTime get startOfMonth => DateTime(year, month, 1);

  /// Days until this date.
  int get daysFromNow =>
      difference(DateTime.now().startOfDay).inDays;

  /// Format as time string (HH:mm).
  String get timeString =>
      '${hour.toString().padLeft(2, '0')}:${minute.toString().padLeft(2, '0')}';
}

extension NumExtensions on num {
  /// Convert to percentage string.
  String toPercentage({int decimals = 1}) => '${toStringAsFixed(decimals)}%';

  /// Format as currency (EGP).
  String toCurrency({String locale = 'ar'}) {
    if (locale == 'ar') return '${toStringAsFixed(0)} ج.م';
    return 'EGP ${toStringAsFixed(0)}';
  }
}

extension ListExtensions<T> on List<T> {
  /// Safely get element at index or null.
  T? getOrNull(int index) {
    if (index < 0 || index >= length) return null;
    return this[index];
  }

  /// Group list by a key.
  Map<K, List<T>> groupBy<K>(K Function(T) keyFunction) {
    final map = <K, List<T>>{};
    for (final element in this) {
      final key = keyFunction(element);
      map.putIfAbsent(key, () => []).add(element);
    }
    return map;
  }
}

extension ContextExtensions on BuildContext {
  /// Get the current theme.
  ThemeData get theme => Theme.of(this);

  /// Get the current text theme.
  TextTheme get textTheme => Theme.of(this).textTheme;

  /// Get the current color scheme.
  ColorScheme get colorScheme => Theme.of(this).colorScheme;

  /// Get the screen size.
  Size get screenSize => MediaQuery.of(this).size;

  /// Get the screen width.
  double get screenWidth => MediaQuery.of(this).size.width;

  /// Get the screen height.
  double get screenHeight => MediaQuery.of(this).size.height;

  /// Check if the current locale is Arabic.
  bool get isArabic => Localizations.localeOf(this).languageCode == 'ar';

  /// Check if the layout is RTL.
  bool get isRtl => Directionality.of(this) == TextDirection.rtl;

  /// Check if dark mode is active.
  bool get isDark => Theme.of(this).brightness == Brightness.dark;

  /// Show a snackbar.
  void showSnackBar(
    String message, {
    Duration duration = const Duration(seconds: 3),
    SnackBarAction? action,
    bool isError = false,
  }) {
    ScaffoldMessenger.of(this).showSnackBar(
      SnackBar(
        content: Text(message),
        duration: duration,
        action: action,
        backgroundColor: isError ? Colors.red : null,
      ),
    );
  }

  /// Show a bottom sheet.
  Future<T?> showAppBottomSheet<T>({required Widget child}) {
    return showModalBottomSheet<T>(
      context: this,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (_) => child,
    );
  }

  /// Check if keyboard is visible.
  bool get isKeyboardVisible => MediaQuery.of(this).viewInsets.bottom > 0;

  /// Get bottom padding (safe area).
  double get bottomPadding => MediaQuery.of(this).padding.bottom;

  /// Get top padding (safe area / status bar).
  double get topPadding => MediaQuery.of(this).padding.top;

  /// Check if tablet.
  bool get isTablet => screenWidth >= 600;
}
