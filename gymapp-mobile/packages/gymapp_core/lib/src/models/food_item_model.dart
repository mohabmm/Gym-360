import 'package:freezed_annotation/freezed_annotation.dart';

part 'food_item_model.freezed.dart';
part 'food_item_model.g.dart';

@freezed
class FoodItemModel with _$FoodItemModel {
  const factory FoodItemModel({
    required String id,
    required String name,
    String? nameAr,
    required String category,
    required int caloriesPer100g,
    required double proteinPer100g,
    required double carbsPer100g,
    required double fatPer100g,
    double? fiberPer100g,
    String? servingSize,
    int? caloriesPerServing,
    String? imageUrl,
    @Default(false) bool isCustom,
  }) = _FoodItemModel;

  factory FoodItemModel.fromJson(Map<String, dynamic> json) =>
      _$FoodItemModelFromJson(json);

  /// Backend `/nutrition/food-database` returns per-serving macros + `nameArabic`
  /// (see `FoodDatabaseItemDto`), not the per-100g/`nameAr` shape this model
  /// declares. This factory scales per-serving values to per-100g using the
  /// advertised serving size (falling back to treating per-serving as per-100g
  /// when the serving size is missing or zero).
  static FoodItemModel fromFoodDatabaseJson(Map<String, dynamic> raw) {
    final servingSize = (raw['servingSize'] as num?)?.toDouble() ?? 0;
    final servingUnit = (raw['servingUnit'] ?? '').toString();
    final scale = servingSize > 0 ? 100 / servingSize : 1.0;
    final cals = (raw['caloriesPerServing'] as num?)?.toDouble() ?? 0;
    final protein = (raw['proteinPerServing'] as num?)?.toDouble() ?? 0;
    final carbs = (raw['carbsPerServing'] as num?)?.toDouble() ?? 0;
    final fat = (raw['fatPerServing'] as num?)?.toDouble() ?? 0;
    return FoodItemModel(
      id: (raw['id'] ?? '').toString(),
      name: (raw['name'] ?? '').toString(),
      nameAr: raw['nameArabic'] as String?,
      category: (raw['category'] ?? '').toString(),
      caloriesPer100g: (cals * scale).round(),
      proteinPer100g: protein * scale,
      carbsPer100g: carbs * scale,
      fatPer100g: fat * scale,
      servingSize: servingSize > 0
          ? '${_trimTrailingZeros(servingSize)}$servingUnit'
          : null,
      caloriesPerServing: cals.round(),
    );
  }
}

String _trimTrailingZeros(double value) {
  if (value == value.roundToDouble()) return value.toInt().toString();
  return value.toString();
}
