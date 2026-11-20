import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_trainer/features/sessions/presentation/pt/record_session/session_time_draft.dart';

void main() {
  final evening = DateTime(2026, 11, 12, 18, 10);

  test('default: today, just finished — start = now − 60 min, rounded down to 15', () {
    final d = SessionTimeDraft(evening);
    expect(d.isToday, isTrue);
    expect(d.start, const TimeOfDay(hour: 17, minute: 0));
    expect(d.minutes, 60);
    expect(d.issue, isNull);
  });

  test('changing the duration keeps "just finished" until the coach picks a time', () {
    final d = SessionTimeDraft(evening)..setMinutes(45);
    expect(d.start, const TimeOfDay(hour: 17, minute: 15));
    d
      ..setStart(const TimeOfDay(hour: 16, minute: 0))
      ..setMinutes(90);
    expect(d.start, const TimeOfDay(hour: 16, minute: 0));
  });

  test('a session that ends in the future is flagged', () {
    final d = SessionTimeDraft(evening)..setStart(const TimeOfDay(hour: 17, minute: 45));
    expect(d.issue, SessionTimeIssue.notEnded);
  });

  test('up to 7 days back is fine, 8 is too old', () {
    final d = SessionTimeDraft(evening)..setDay(DateTime(2026, 11, 5));
    expect(d.issue, isNull);
    d.setDay(DateTime(2026, 11, 4));
    expect(d.issue, SessionTimeIssue.tooOld);
  });

  test('just after midnight the default falls on yesterday', () {
    final d = SessionTimeDraft(DateTime(2026, 11, 12, 0, 20));
    expect(d.isYesterday, isTrue);
    expect(d.start, const TimeOfDay(hour: 23, minute: 15));
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
