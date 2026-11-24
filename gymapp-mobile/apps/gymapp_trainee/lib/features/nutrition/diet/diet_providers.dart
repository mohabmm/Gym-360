import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../core/providers.dart';

DietPlanModel? _plan(dynamic data) => data is Map && data.containsKey('id') ? DietPlanModel.fromJson(Map<String, dynamic>.from(data)) : null;

TodayMealsModel? _today(dynamic data) => data is Map && data.containsKey('planId') ? TodayMealsModel.fromJson(Map<String, dynamic>.from(data)) : null;

/// The diet the coach gave (null = none yet).
final myDietPlanProvider = FutureProvider.autoDispose<DietPlanModel?>((ref) async {
  return ref.watch(apiClientProvider).get<DietPlanModel?>(ApiEndpoints.myMealPlan, parser: _plan);
});

/// Today's meals from the diet with what the member already ticked (null = no diet yet).
final todayMealsProvider = FutureProvider.autoDispose<TodayMealsModel?>((ref) async {
  return ref.watch(apiClientProvider).get<TodayMealsModel?>(ApiEndpoints.myMealsToday, parser: _today);
});

/// "أكلتها ✓": logs the meal (or its alternative) into today's food log, once.
class DietActions {
  final Ref _ref;

  DietActions(this._ref);

  Future<TodayMealsModel?> ate(int index, {bool alternative = false}) async {
    final today = await _ref.read(apiClientProvider).post<TodayMealsModel?>(
          ApiEndpoints.myMealDone(index),
          data: {'alternative': alternative},
          parser: _today,
        );
    _ref.invalidate(todayMealsProvider);
    return today;
  }
}

final dietActionsProvider = Provider<DietActions>(DietActions.new);
