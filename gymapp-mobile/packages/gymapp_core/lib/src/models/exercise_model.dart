import 'package:freezed_annotation/freezed_annotation.dart';

part 'exercise_model.freezed.dart';
part 'exercise_model.g.dart';

@freezed
class ExerciseModel with _$ExerciseModel {
  const ExerciseModel._();

  const factory ExerciseModel({
    required String id,
    required String name,
    String? nameAr,
    String? description,
    String? instructions,
    String? demoMediaUrl,
    @Default(<String>[]) List<String> muscleGroups,
    required String equipment,
    required String category,
    required String scope,
    String? gymId,
    String? createdBy,
    @Default(true) bool isActive,
  }) = _ExerciseModel;

  factory ExerciseModel.fromJson(Map<String, dynamic> json) =>
      _$ExerciseModelFromJson(json);

  /// Convenience: first muscle group, or empty string if none.
  /// BE's PagedResponse returns `muscleGroups: List<string>`; UI historically
  /// showed a single label, so we expose the first entry here.
  String get muscleGroup =>
      muscleGroups.isNotEmpty ? muscleGroups.first : '';

  bool get isCustom => scope.toLowerCase() == 'custom';
}

@freezed
class ExerciseFilterModel with _$ExerciseFilterModel {
  const factory ExerciseFilterModel({
    String? search,
    String? muscleGroup,
    String? equipment,
    String? category,
    String? difficulty,
    @Default(false) bool customOnly,
  }) = _ExerciseFilterModel;

  factory ExerciseFilterModel.fromJson(Map<String, dynamic> json) =>
      _$ExerciseFilterModelFromJson(json);
}
