import 'package:freezed_annotation/freezed_annotation.dart';
import 'pagination_model.dart';

part 'api_response_model.freezed.dart';
part 'api_response_model.g.dart';

/// Backend error entry per docs/01_GYMAPP_BACKEND_COMPLETE.md §"Error Response for Gated Features":
///   { "code": "PLAN.FEATURE_NOT_AVAILABLE", "message": "...", "meta": { ... } }
@freezed
class ApiErrorModel with _$ApiErrorModel {
  const factory ApiErrorModel({
    required String code,
    required String message,
    Map<String, dynamic>? meta,
  }) = _ApiErrorModel;

  factory ApiErrorModel.fromJson(Map<String, dynamic> json) =>
      _$ApiErrorModelFromJson(json);
}

@freezed
class ApiResponseModel<T> with _$ApiResponseModel<T> {
  const factory ApiResponseModel({
    T? data,
    @Default(<ApiErrorModel>[]) List<ApiErrorModel> errors,
    PaginationMeta? meta,
  }) = _ApiResponseModel<T>;
}

/// Non-generic version for JSON deserialization.
@freezed
class ApiResponseRaw with _$ApiResponseRaw {
  const factory ApiResponseRaw({
    dynamic data,
    @Default(<ApiErrorModel>[]) List<ApiErrorModel> errors,
    Map<String, dynamic>? meta,
  }) = _ApiResponseRaw;

  factory ApiResponseRaw.fromJson(Map<String, dynamic> json) =>
      _$ApiResponseRawFromJson(json);
}
