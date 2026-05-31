import 'package:freezed_annotation/freezed_annotation.dart';

part 'streak_model.freezed.dart';
part 'streak_model.g.dart';

@freezed
class StreakModel with _$StreakModel {
  const factory StreakModel({
    required String id,
    required String userId,
    required int currentStreak,
    required int longestStreak,
    DateTime? lastActivityDate,
    required String streakType,
  }) = _StreakModel;

  factory StreakModel.fromJson(Map<String, dynamic> json) =>
      _$StreakModelFromJson(json);
}
