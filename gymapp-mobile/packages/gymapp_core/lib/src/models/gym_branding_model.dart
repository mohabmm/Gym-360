import 'package:freezed_annotation/freezed_annotation.dart';

part 'gym_branding_model.freezed.dart';
part 'gym_branding_model.g.dart';

@freezed
class GymBrandingModel with _$GymBrandingModel {
  const factory GymBrandingModel({
    String? logoUrl,
    @Default('#10B981') String primaryColor,
    @Default('#1A1A1A') String secondaryColor,
    String? coverPhotoUrl,
    String? faviconUrl,
  }) = _GymBrandingModel;

  factory GymBrandingModel.fromJson(Map<String, dynamic> json) =>
      _$GymBrandingModelFromJson(json);
}
