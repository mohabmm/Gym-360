import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_trainer/features/sessions/presentation/pt/record_session/session_time_draft.dart';

void main() {
  final evening = DateTime(2026, 11, 12, 18, 10);

  test('default: today, going on now — start = now, rounded down to 15', () {
    final d = SessionTimeDraft(evening);
    expect(d.isToday, isTrue);
    expect(d.isNow, isTrue);
    expect(d.start, const TimeOfDay(hour: 18, minute: 0));
    expect(d.minutes, 60);
    expect(d.issue, isNull, reason: 'the coach records the session while it is going on');
  });

  test('changing the duration keeps the start time', () {
    final d = SessionTimeDraft(evening)..setMinutes(45);
    expect(d.start, const TimeOfDay(hour: 18, minute: 0));
    d
      ..setStart(const TimeOfDay(hour: 16, minute: 0))
      ..setMinutes(90);
    expect(d.start, const TimeOfDay(hour: 16, minute: 0));
  });

  test('a session in progress is fine, one that has not started is flagged', () {
    final d = SessionTimeDraft(evening)..setStart(const TimeOfDay(hour: 17, minute: 45));
    expect(d.issue, isNull);
    d.setStart(const TimeOfDay(hour: 18, minute: 15));
    expect(d.issue, isNull, reason: 'within the 5-minute slack');
    d.setStart(const TimeOfDay(hour: 18, minute: 30));
    expect(d.issue, SessionTimeIssue.notStarted);
  });

  test('"Now" undoes a picked day and time', () {
    final d = SessionTimeDraft(evening)
      ..setDay(DateTime(2026, 11, 10))
      ..setStart(const TimeOfDay(hour: 9, minute: 0));
    expect(d.isNow, isFalse);
    d.resetToNow();
    expect(d.isNow, isTrue);
    expect(d.day, DateTime(2026, 11, 12));
    expect(d.start, const TimeOfDay(hour: 18, minute: 0));
  });

  test('up to 7 days back is fine, 8 is too old', () {
    final d = SessionTimeDraft(evening)..setDay(DateTime(2026, 11, 5));
    expect(d.issue, isNull);
    d.setDay(DateTime(2026, 11, 4));
    expect(d.issue, SessionTimeIssue.tooOld);
  });

  test('just after midnight the default is today, 00:15', () {
    final d = SessionTimeDraft(DateTime(2026, 11, 12, 0, 20));
    expect(d.isToday, isTrue);
    expect(d.start, const TimeOfDay(hour: 0, minute: 15));
  });

  test('payload is gym-local date, HH:mm:00 and minutes', () {
    final d = SessionTimeDraft(evening)
      ..setDay(DateTime(2026, 11, 11))
      ..setStart(const TimeOfDay(hour: 9, minute: 5))
      ..setMinutes(45);
    expect(d.toPayload(), {'date': '2026-11-11', 'startTime': '09:05:00', 'durationMinutes': 45});
  });

  test('server result keeps the recorded slot', () {
    final r = RecordedSession.fromJson(
        {'sessionId': 's1', 'remainingSessions': 7, 'alreadyLogged': true, 'date': '2026-11-11', 'startTime': '18:00:00'});
    expect(r.startAt, DateTime(2026, 11, 11, 18));
    expect(r.alreadyLogged, isTrue);
  });
}
