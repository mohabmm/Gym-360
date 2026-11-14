import 'package:flutter/material.dart';

/// Why the chosen time can't be recorded.
enum SessionTimeIssue { tooOld, notEnded }

/// When a PT session happened, as the coach picks it in the "Record session" sheet.
///
/// Default: today, *just finished* — start = now − duration, rounded down to 15 minutes.
/// The coach may pick any day up to [maxDaysBack] days back (server rule, same number).
class SessionTimeDraft {
  static const int maxDaysBack = 7;
  static const List<int> durations = [30, 45, 60, 90];
  static const Duration _endSlack = Duration(minutes: 5);

  final DateTime now;
  late DateTime _day;
  late TimeOfDay _start;
  int _minutes = 60;
  bool _startPicked = false;

  SessionTimeDraft(this.now) {
    _resetToJustFinished();
  }

  DateTime get day => _day;
  TimeOfDay get start => _start;
  int get minutes => _minutes;

  DateTime get today => DateUtils.dateOnly(now);
  DateTime get yesterday => today.subtract(const Duration(days: 1));
  DateTime get firstDay => today.subtract(const Duration(days: maxDaysBack));

  bool get isToday => DateUtils.isSameDay(_day, today);
  bool get isYesterday => DateUtils.isSameDay(_day, yesterday);

  DateTime get startAt => DateTime(_day.year, _day.month, _day.day, _start.hour, _start.minute);
  DateTime get endAt => startAt.add(Duration(minutes: _minutes));

  SessionTimeIssue? get issue {
    if (_day.isBefore(firstDay)) return SessionTimeIssue.tooOld;
    if (endAt.isAfter(now.add(_endSlack))) return SessionTimeIssue.notEnded;
    return null;
  }

  void setDay(DateTime day) {
    _day = DateUtils.dateOnly(day);
    // A past day has no "just finished": keep the time unless the coach never picked one and
    // goes back to today.
    if (isToday && !_startPicked) _resetToJustFinished();
  }

  void setStart(TimeOfDay start) {
    _start = start;
    _startPicked = true;
  }

  void setMinutes(int minutes) {
    _minutes = minutes;
    if (isToday && !_startPicked) _resetToJustFinished();
  }

  void _resetToJustFinished() {
    final begun = now.subtract(Duration(minutes: _minutes));
    _day = DateUtils.dateOnly(begun);
    _start = TimeOfDay(hour: begun.hour, minute: begun.minute - begun.minute % 15);
  }

  static String _two(int v) => v.toString().padLeft(2, '0');

  /// `date`, `startTime` and `durationMinutes` for `POST scheduling/sessions/quick-log` (gym-local).
  Map<String, dynamic> toPayload() => {
        'date': '${_day.year.toString().padLeft(4, '0')}-${_two(_day.month)}-${_two(_day.day)}',
        'startTime': '${_two(_start.hour)}:${_two(_start.minute)}:00',
        'durationMinutes': _minutes,
      };
}

/// The server's answer to a recorded session.
class RecordedSession {
  final String sessionId;
  final int remaining;
  final bool alreadyLogged;
  final DateTime? startAt;

  const RecordedSession({required this.sessionId, required this.remaining, required this.alreadyLogged, this.startAt});

  factory RecordedSession.fromJson(Map<String, dynamic> json) {
    final date = DateTime.tryParse(json['date']?.toString() ?? '');
    final time = (json['startTime']?.toString() ?? '').split(':');
    final hasTime = date != null && time.length >= 2;
    return RecordedSession(
      sessionId: json['sessionId']?.toString() ?? '',
      remaining: (json['remainingSessions'] as num?)?.toInt() ?? 0,
      alreadyLogged: json['alreadyLogged'] == true,
      startAt: hasTime ? DateTime(date.year, date.month, date.day, int.parse(time[0]), int.parse(time[1])) : null,
    );
  }
}
