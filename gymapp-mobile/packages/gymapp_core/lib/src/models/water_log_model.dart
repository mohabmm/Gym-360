import 'package:freezed_annotation/freezed_annotation.dart';

part 'water_log_model.freezed.dart';
part 'water_log_model.g.dart';

@freezed
class WaterLogModel with _$WaterLogModel {
  const factory WaterLogModel({
    required String id,
    required String userId,
    required DateTime date,
    required int totalMl,
    required int targetMl,
    required List<WaterEntryModel> entries,
  }) = _WaterLogModel;

  factory WaterLogModel.fromJson(Map<String, dynamic> json) =>
      _$WaterLogModelFromJson(json);
}

@freezed
class WaterEntryModel with _$WaterEntryModel {
  const factory WaterEntryModel({
    required String id,
    required int amountMl,
    required DateTime loggedAt,
  }) = _WaterEntryModel;

  factory WaterEntryModel.fromJson(Map<String, dynamic> json) =>
      _$WaterEntryModelFromJson(json);
}
