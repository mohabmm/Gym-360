import 'package:freezed_annotation/freezed_annotation.dart';

part 'progress_photo_model.freezed.dart';
part 'progress_photo_model.g.dart';

@freezed
class ProgressPhotoModel with _$ProgressPhotoModel {
  const factory ProgressPhotoModel({
    required String id,
    required String userId,
    required String type,
    required String imageUrl,
    String? thumbnailUrl,
    DateTime? takenAt,
    @Default(false) bool isSharedWithTrainer,
    String? notes,
    DateTime? createdAt,
  }) = _ProgressPhotoModel;

  factory ProgressPhotoModel.fromJson(Map<String, dynamic> json) =>
      _$ProgressPhotoModelFromJson(json);
}

@freezed
class ProgressPhotoSetModel with _$ProgressPhotoSetModel {
  const factory ProgressPhotoSetModel({
    required DateTime date,
    ProgressPhotoModel? front,
    ProgressPhotoModel? side,
    ProgressPhotoModel? back,
  }) = _ProgressPhotoSetModel;

  factory ProgressPhotoSetModel.fromJson(Map<String, dynamic> json) =>
      _$ProgressPhotoSetModelFromJson(json);
}
