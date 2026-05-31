import 'package:freezed_annotation/freezed_annotation.dart';

part 'trainer_rating_model.freezed.dart';
part 'trainer_rating_model.g.dart';

@freezed
class TrainerRatingModel with _$TrainerRatingModel {
  const factory TrainerRatingModel({
    required String id,
    required String trainerId,
    required String memberId,
    String? memberName,
    required String sessionId,
    required int stars,
    String? comment,
    DateTime? createdAt,
  }) = _TrainerRatingModel;

  factory TrainerRatingModel.fromJson(Map<String, dynamic> json) =>
      _$TrainerRatingModelFromJson(json);
}

@freezed
class RateTrainerRequest with _$RateTrainerRequest {
  const factory RateTrainerRequest({
    required String gymId,
    required String trainerId,
    required String clientId,
    required String sessionId,
    required int stars,
    String? comment,
  }) = _RateTrainerRequest;

  factory RateTrainerRequest.fromJson(Map<String, dynamic> json) =>
      _$RateTrainerRequestFromJson(json);
}

@freezed
class TrainerPublicProfileModel with _$TrainerPublicProfileModel {
  const factory TrainerPublicProfileModel({
    required String trainerId,
    required String trainerName,
    String? trainerPhotoUrl,
    required double averageRating,
    required int totalReviews,
    List<TrainerBadgeModel>? badges,
    List<TrainerTestimonialModel>? testimonials,
    int? totalClients,
    int? totalSessions,
    String? specialization,
    String? bio,
  }) = _TrainerPublicProfileModel;

  factory TrainerPublicProfileModel.fromJson(Map<String, dynamic> json) =>
      _$TrainerPublicProfileModelFromJson(json);
}

@freezed
class TrainerBadgeModel with _$TrainerBadgeModel {
  const factory TrainerBadgeModel({
    required String id,
    required String name,
    String? nameAr,
    required String type,
    required String month,
    String? iconUrl,
  }) = _TrainerBadgeModel;

  factory TrainerBadgeModel.fromJson(Map<String, dynamic> json) =>
      _$TrainerBadgeModelFromJson(json);
}

@freezed
class TrainerTestimonialModel with _$TrainerTestimonialModel {
  const factory TrainerTestimonialModel({
    required String id,
    required String content,
    required int stars,
    required String status,
    DateTime? createdAt,
    DateTime? approvedAt,
  }) = _TrainerTestimonialModel;

  factory TrainerTestimonialModel.fromJson(Map<String, dynamic> json) =>
      _$TrainerTestimonialModelFromJson(json);
}

@freezed
class TrainerMonthlyScoreModel with _$TrainerMonthlyScoreModel {
  const factory TrainerMonthlyScoreModel({
    required String month,
    required double averageRating,
    required double normalizedScore,
    required int sessionsCompleted,
    required int uniqueClients,
  }) = _TrainerMonthlyScoreModel;

  factory TrainerMonthlyScoreModel.fromJson(Map<String, dynamic> json) =>
      _$TrainerMonthlyScoreModelFromJson(json);
}
