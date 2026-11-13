import '../models/body_metric_model.dart';

/// What the member is training for (backend `ProgressGoalType`).
enum ProgressGoalType {
  loseFat('LoseFat'),
  buildMuscle('BuildMuscle'),
  generalFitness('GeneralFitness');

  const ProgressGoalType(this.apiValue);

  /// Value sent to and received from the API.
  final String apiValue;

  static ProgressGoalType? tryParse(String? raw) {
    if (raw == null) return null;
    for (final g in values) {
      if (g.apiValue.toLowerCase() == raw.toLowerCase()) return g;
    }
    return null;
  }
}

/// Backend `ProgressGoalDto`.
class ProgressGoalModel {
  final ProgressGoalType goal;
  final double? targetWeight;
  final String setBy;
  final DateTime? updatedAt;

  const ProgressGoalModel({
    required this.goal,
    this.targetWeight,
    this.setBy = '',
    this.updatedAt,
  });

  /// Null when the goal value is unknown (treated as "no goal").
  static ProgressGoalModel? tryFromJson(Object? json) {
    if (json is! Map) return null;
    final goal = ProgressGoalType.tryParse(json['goal']?.toString());
    if (goal == null) return null;
    return ProgressGoalModel(
      goal: goal,
      targetWeight: (json['targetWeight'] as num?)?.toDouble(),
      setBy: json['setBy']?.toString() ?? '',
      updatedAt: DateTime.tryParse(json['updatedAt']?.toString() ?? ''),
    );
  }
}

/// Backend `ProgressJourneyResponse` from `GET /progress/journey` and
/// `GET /progress/clients/{id}/journey`: every entry since day one + the goal.
class ProgressJourneyModel {
  final String userId;
  final List<BodyMetricEntryModel> entries;
  final ProgressGoalModel? goal;

  const ProgressJourneyModel({
    required this.userId,
    required this.entries,
    this.goal,
  });

  factory ProgressJourneyModel.fromJson(Map<String, dynamic> json) {
    final raw = json['entries'];
    return ProgressJourneyModel(
      userId: json['userId']?.toString() ?? '',
      entries: raw is List
          ? raw
              .map((e) => BodyMetricEntryModel.fromJson(Map<String, dynamic>.from(e as Map)))
              .toList()
          : const [],
      goal: ProgressGoalModel.tryFromJson(json['goal']),
    );
  }
}
