import 'exercise_progress_model.dart';

/// pt.exercise_progress: one logged session on the member's board (backend `SessionSummaryDto`).
class SessionSummaryModel {
  final String sessionId;
  final int sessionNumber;
  final DateTime date;

  /// The coach's label ("Push Day"); null for sessions without one.
  final String? dayLabel;
  final bool isTrialSession;

  /// Keys into [ExerciseProgressModel.exercises], in the coach's order.
  final List<String> exerciseKeys;

  /// The coach's post-session log; null until they write one.
  final SessionCoachRecapModel? coachRecap;

  const SessionSummaryModel({
    required this.sessionId,
    required this.sessionNumber,
    required this.date,
    this.dayLabel,
    this.isTrialSession = false,
    this.exerciseKeys = const [],
    this.coachRecap,
  });

  factory SessionSummaryModel.fromJson(Map<String, dynamic> json) => SessionSummaryModel(
        sessionId: json['sessionId']?.toString() ?? '',
        sessionNumber: (json['sessionNumber'] as num?)?.toInt() ?? 0,
        date: DateTime.tryParse(json['date']?.toString() ?? '') ?? DateTime.now(),
        dayLabel: json['dayLabel']?.toString(),
        isTrialSession: json['isTrialSession'] == true,
        exerciseKeys: (json['exerciseKeys'] as List? ?? const []).map((e) => e.toString()).toList(),
        coachRecap: SessionCoachRecapModel.tryFromJson(json['coachRecap']),
      );

  /// Older servers send no `sessions`: rebuild them from the exercise entries (no coach recap).
  static List<SessionSummaryModel> fromEntries(List<ExerciseHistoryModel> exercises) {
    final byId = <String, SessionSummaryModel>{};
    for (final exercise in exercises) {
      for (final entry in exercise.entries) {
        final known = byId[entry.sessionId];
        byId[entry.sessionId] = SessionSummaryModel(
          sessionId: entry.sessionId,
          sessionNumber: entry.sessionNumber,
          date: entry.date,
          dayLabel: entry.dayLabel,
          isTrialSession: entry.isTrialSession,
          exerciseKeys: [...?known?.exerciseKeys, exercise.key],
        );
      }
    }
    return byId.values.toList()..sort((a, b) => a.sessionNumber.compareTo(b.sessionNumber));
  }
}

/// What the coach wrote after the session.
class SessionCoachRecapModel {
  final String? feedback;
  final String? nextSessionFocus;

  /// How hard the member worked, 1–5.
  final int effortRating;

  const SessionCoachRecapModel({this.feedback, this.nextSessionFocus, this.effortRating = 0});

  static SessionCoachRecapModel? tryFromJson(Object? json) {
    if (json is! Map) return null;
    return SessionCoachRecapModel(
      feedback: _text(json['feedback']),
      nextSessionFocus: _text(json['nextSessionFocus']),
      effortRating: (json['effortRating'] as num?)?.toInt() ?? 0,
    );
  }

  bool get hasFeedback => feedback != null;
  bool get hasFocus => nextSessionFocus != null;

  static String? _text(Object? raw) {
    final value = raw?.toString().trim();
    return value == null || value.isEmpty ? null : value;
  }
}
