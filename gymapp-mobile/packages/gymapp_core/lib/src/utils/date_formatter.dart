import 'package:intl/intl.dart';

/// Date formatting utilities with Arabic locale support.
class DateFormatter {
  DateFormatter._();

  /// The API serialises timestamps in UTC without a `Z` suffix, so
  /// `DateTime.parse` reads them as local wall-clock time. Re-interpret such a
  /// value as UTC and convert it to the device's zone before display.
  static DateTime apiUtcToLocal(DateTime value) {
    if (value.isUtc) return value.toLocal();
    return DateTime.utc(
      value.year,
      value.month,
      value.day,
      value.hour,
      value.minute,
      value.second,
      value.millisecond,
      value.microsecond,
    ).toLocal();
  }

  /// Format date as "12 يناير 2024" (Arabic) or "12 January 2024" (English).
  static String formatLong(DateTime date, {String locale = 'ar'}) {
    return DateFormat('dd MMMM yyyy', locale).format(date);
  }

  /// Format date as "12/01/2024".
  static String formatShort(DateTime date) {
    return DateFormat('dd/MM/yyyy').format(date);
  }

  /// Format time as "09:30 ص" (Arabic) or "09:30 AM" (English).
  static String formatTime(DateTime date, {String locale = 'ar'}) {
    return DateFormat('hh:mm a', locale).format(date);
  }

  /// Format time in 24h format.
  static String formatTime24(DateTime date) {
    return DateFormat('HH:mm').format(date);
  }

  /// Format as relative time: "منذ 5 دقائق" / "5 minutes ago".
  static String formatRelative(DateTime date, {String locale = 'ar'}) {
    final now = DateTime.now();
    final diff = now.difference(date);

    if (diff.inSeconds < 60) {
      return locale == 'ar' ? 'الآن' : 'Just now';
    }
    if (diff.inMinutes < 60) {
      final mins = diff.inMinutes;
      return locale == 'ar' ? 'منذ $mins دقيقة' : '${mins}m ago';
    }
    if (diff.inHours < 24) {
      final hours = diff.inHours;
      return locale == 'ar' ? 'منذ $hours ساعة' : '${hours}h ago';
    }
    if (diff.inDays < 7) {
      final days = diff.inDays;
      return locale == 'ar' ? 'منذ $days يوم' : '${days}d ago';
    }
    return formatShort(date);
  }

  /// Format date and time together.
  static String formatDateTime(DateTime date, {String locale = 'ar'}) {
    return '${formatShort(date)} ${formatTime(date, locale: locale)}';
  }

  /// Format day of week: "السبت" / "Saturday".
  static String formatDayOfWeek(int dayOfWeek, {String locale = 'ar'}) {
    // Create a DateTime for the given day (using a known Saturday = 6)
    final date = DateTime(2024, 1, dayOfWeek); // Jan 1 2024 is Monday
    return DateFormat('EEEE', locale).format(date);
  }

  /// Format month and year: "يناير 2024" / "January 2024".
  static String formatMonthYear(DateTime date, {String locale = 'ar'}) {
    return DateFormat('MMMM yyyy', locale).format(date);
  }

  /// Check if a date is today.
  static bool isToday(DateTime date) {
    final now = DateTime.now();
    return date.year == now.year &&
        date.month == now.month &&
        date.day == now.day;
  }

  /// Check if a date is tomorrow.
  static bool isTomorrow(DateTime date) {
    final tomorrow = DateTime.now().add(const Duration(days: 1));
    return date.year == tomorrow.year &&
        date.month == tomorrow.month &&
        date.day == tomorrow.day;
  }

  /// Get greeting based on time of day.
  static String getGreeting({String locale = 'ar'}) {
    final hour = DateTime.now().hour;
    if (hour < 12) {
      return locale == 'ar' ? 'صباح الخير' : 'Good morning';
    }
    if (hour < 17) {
      return locale == 'ar' ? 'مساء الخير' : 'Good afternoon';
    }
    return locale == 'ar' ? 'مساء الخير' : 'Good evening';
  }

  /// Format duration in minutes to "1h 30m" or "30m".
  static String formatDuration(int minutes, {String locale = 'ar'}) {
    if (minutes < 60) {
      return locale == 'ar' ? '$minutes د' : '${minutes}m';
    }
    final hours = minutes ~/ 60;
    final mins = minutes % 60;
    if (mins == 0) {
      return locale == 'ar' ? '$hours س' : '${hours}h';
    }
    return locale == 'ar' ? '$hours س $mins د' : '${hours}h ${mins}m';
  }
}
