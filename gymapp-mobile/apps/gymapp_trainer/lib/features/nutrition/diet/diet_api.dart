import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../core/providers.dart';

Map<String, dynamic> _json(dynamic data) => Map<String, dynamic>.from(data as Map);

/// Ready Egyptian diets, the ones for [goal] first.
final readyDietsProvider = FutureProvider.autoDispose.family<List<ReadyDietSummaryModel>, DietGoal>((ref, goal) async {
  return ref.watch(apiClientProvider).get<List<ReadyDietSummaryModel>>(
        ApiEndpoints.readyDiets,
        queryParameters: {'goal': goal.api},
        parser: (data) => [for (final item in data as List) ReadyDietSummaryModel.fromJson(_json(item))],
      );
});

/// A ready diet's week scaled to the calories.
final readyDietPreviewProvider =
    FutureProvider.autoDispose.family<ReadyDietPreviewModel, ({String key, int calories})>((ref, args) async {
  return ref.watch(apiClientProvider).get<ReadyDietPreviewModel>(
        ApiEndpoints.readyDietByKey(args.key),
        queryParameters: {'calories': args.calories},
        parser: (data) => ReadyDietPreviewModel.fromJson(_json(data)),
      );
});

/// The client's current diet and their last 7 days.
final clientDietProvider = FutureProvider.autoDispose.family<ClientDietModel, String>((ref, clientId) async {
  return ref.watch(apiClientProvider).get<ClientDietModel>(
        ApiEndpoints.clientDiet(clientId),
        parser: (data) => ClientDietModel.fromJson(_json(data)),
      );
});

/// The calculator and giving a diet.
class DietApi {
  final Ref _ref;

  DietApi(this._ref);

  Future<DailyTargetModel> calculate(BodyInputModel body) => _ref.read(apiClientProvider).post<DailyTargetModel>(
        ApiEndpoints.calorieTarget,
        data: body.toJson(),
        parser: (data) => DailyTargetModel.fromJson(_json(data)),
      );

  /// Gives [clientId] the ready diet [dietKey] scaled to [target]; the member is notified.
  Future<void> give({required String clientId, required String dietKey, required DailyTargetModel target}) async {
    await _ref.read(apiClientProvider).postVoid(
          ApiEndpoints.clientDiet(clientId),
          data: {'dietKey': dietKey, 'target': target.toTargetJson()},
        );
    _ref.invalidate(clientDietProvider(clientId));
  }
}

final dietApiProvider = Provider<DietApi>(DietApi.new);
