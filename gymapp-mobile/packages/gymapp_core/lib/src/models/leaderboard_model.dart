import 'package:freezed_annotation/freezed_annotation.dart';

part 'leaderboard_model.freezed.dart';
part 'leaderboard_model.g.dart';

@freezed
class LeaderboardEntryModel with _$LeaderboardEntryModel {
  const factory LeaderboardEntryModel({
    required int position,
    required String memberId,
    required String memberName,
    String? memberPhotoUrl,
    required double score,
    String? scoreLabel,
  }) = _LeaderboardEntryModel;

  factory LeaderboardEntryModel.fromJson(Map<String, dynamic> json) =>
      _$LeaderboardEntryModelFromJson(json);
}

@freezed
class LeaderboardModel with _$LeaderboardModel {
  const factory LeaderboardModel({
    required String type,
    required String period,
    required List<LeaderboardEntryModel> entries,
    LeaderboardEntryModel? myPosition,
    int? totalParticipants,
    int? optedOutCount,
  }) = _LeaderboardModel;

  factory LeaderboardModel.fromJson(Map<String, dynamic> json) =>
      _$LeaderboardModelFromJson(json);
}

@freezed
class TrainerLeaderboardEntryModel with _$TrainerLeaderboardEntryModel {
  const factory TrainerLeaderboardEntryModel({
    required int rank,
    required String trainerId,
    required String trainerName,
    String? trainerPhotoUrl,
    required double normalizedScore,
    required double averageRating,
    required int sessionsThisMonth,
    required int uniqueClients,
    @Default(false) bool isCoachOfMonth,
  }) = _TrainerLeaderboardEntryModel;

  factory TrainerLeaderboardEntryModel.fromJson(Map<String, dynamic> json) =>
      _$TrainerLeaderboardEntryModelFromJson(json);
}
