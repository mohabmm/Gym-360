import 'package:freezed_annotation/freezed_annotation.dart';

part 'announcement_model.freezed.dart';
part 'announcement_model.g.dart';

@freezed
class AnnouncementModel with _$AnnouncementModel {
  const factory AnnouncementModel({
    required String id,
    required String gymId,
    required String title,
    String? titleAr,
    required String content,
    String? contentAr,
    String? imageUrl,
    required String category,
    String? targetAudience,
    String? status,
    String? createdBy,
    String? createdByName,
    DateTime? scheduledAt,
    DateTime? publishedAt,
    DateTime? createdAt,
  }) = _AnnouncementModel;

  factory AnnouncementModel.fromJson(Map<String, dynamic> json) =>
      _$AnnouncementModelFromJson(json);
}

@freezed
class CreateAnnouncementRequest with _$CreateAnnouncementRequest {
  const factory CreateAnnouncementRequest({
    required String title,
    String? titleAr,
    required String content,
    String? contentAr,
    String? imageUrl,
    required String category,
    String? targetAudience,
    DateTime? scheduledAt,
  }) = _CreateAnnouncementRequest;

  factory CreateAnnouncementRequest.fromJson(Map<String, dynamic> json) =>
      _$CreateAnnouncementRequestFromJson(json);
}
