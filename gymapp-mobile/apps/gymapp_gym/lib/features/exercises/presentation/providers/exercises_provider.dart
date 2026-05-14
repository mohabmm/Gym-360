import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';

final exercisesProvider =
    FutureProvider.autoDispose<List<ExerciseModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  return apiClient.get<List<ExerciseModel>>(
    ApiEndpoints.exercises,
    queryParameters: {'page': 1, 'pageSize': 100},
    parser: (data) {
      final List<dynamic> list;
      if (data is List) {
        list = data;
      } else if (data is Map<String, dynamic> && data['items'] is List) {
        list = data['items'] as List<dynamic>;
      } else {
        return <ExerciseModel>[];
      }
      return list
          .map((e) => ExerciseModel.fromJson(e as Map<String, dynamic>))
          .toList();
    },
  );
});

final customExercisesProvider =
    FutureProvider.autoDispose<List<ExerciseModel>>((ref) async {
  final apiClient = ref.watch(apiClientProvider);
  final gymId = ref.watch(currentGymIdProvider);
  return apiClient.get<List<ExerciseModel>>(
    ApiEndpoints.customExercises,
    queryParameters: {'gymId': gymId},
    parser: (data) {
      final List<dynamic> list;
      if (data is List) {
        list = data;
      } else if (data is Map<String, dynamic> && data['items'] is List) {
        list = data['items'] as List<dynamic>;
      } else {
        return <ExerciseModel>[];
      }
      return list
          .map((e) => ExerciseModel.fromJson(e as Map<String, dynamic>))
          .toList();
    },
  );
});
